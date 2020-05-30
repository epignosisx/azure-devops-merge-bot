using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace MergeBot
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public string JwtSigningKey => Configuration["JwtSigningKey"];
        public string JwtIssuer => Configuration["JwtIssuer"];

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var httpClient = new HttpClient(new SocketsHttpHandler { UseCookies = false });

            services.Configure<ExtensionSettings>(Configuration);
            services.AddSingleton<HttpClient>(httpClient);
            services.AddSingleton<IMergePolicyRunnerFactory, MergePolicyRunnerFactory>();
            services.AddSingleton<IAzureDevOpsClientFactory, AzureDevOpsClientFactory>();
            services.AddSingleton<IPullRequestMonitor>(c => new PullRequestMonitor(c.GetService<ILogger<PullRequestMonitor>>()));
            services.AddSingleton<ReleaseBranchCascadingPolicy>();
            services.AddSingleton<SpecificSourceAndTargetPolicy>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
                        ValidateIssuerSigningKey = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidIssuer = JwtIssuer,
                    };
                });

            services.AddAuthorization();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IMergePolicyRunnerFactory runnerFactory,
            IAzureDevOpsClientFactory azDoClientFactory,
            ILogger<Startup> logger)
        {
            app.UseSerilogRequestLogging();
            app.Use(ExceptionHandler);
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", Home);
                endpoints.MapPost("/jwt", JwtRoute);
                endpoints.MapPost("/webhook", Webhook).RequireAuthorization();
            });

            async Task Home(HttpContext context)
            {
                await context.Response.WriteAsync("Merge-a-Bot");
            }

            async Task JwtRoute(HttpContext context)
            {
                using var sr = new StreamReader(context.Request.Body);
                var pat = await sr.ReadToEndAsync();
                if (string.IsNullOrEmpty(pat))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Body did not contain Personal Access Token");
                    return;
                }

                var claims = new List<Claim> {
                    new Claim(JwtRegisteredClaimNames.Sub, pat),
                };

                var token = new JwtSecurityToken(
                    issuer: JwtIssuer,
                    audience: null,
                    claims: claims,
                    expires: DateTime.UtcNow.Date.AddYears(1),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)), SecurityAlgorithms.HmacSha256)
                );

                var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
                await context.Response.WriteAsync(tokenValue);
            }

            async Task Webhook(HttpContext context)
            {
                var request = context.Request;
                if (!HttpMethods.IsPost(request.Method))
                    return;

                if (request.ContentType == null || !request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                    return;

                var payload = await WebhookDeserializer.DeserializeAsync(request.Body);
                var pat = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var azDoClient = azDoClientFactory.Create(pat);
                if (payload != null)
                {
                    logger.LogInformation("Received request for {RepositoryUrl}", payload.Resource.Repository.Url);
                    var organization = new Uri(payload.Resource.Repository.Url).Segments[1].TrimEnd('/');
                    var factoryContext = new MergePolicyRunnerFactoryContext(azDoClient, payload.Resource.Repository.Id, organization);
                    var runner = await runnerFactory.CreateAsync(factoryContext);
                    await runner.RunAsync(azDoClient, payload);
                }
            }

            async Task ExceptionHandler(HttpContext context, Func<Task> next)
            {
                try
                {
                    await next();
                }
                catch (Exception ex)
                {
                    logger.LogError(new EventId(1, "UnhandledError"), ex, "Unhandled error");
                    context.Response.StatusCode = 200;
                }
            }
        }
    }
}

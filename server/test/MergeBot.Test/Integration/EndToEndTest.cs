using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Vcr;
using Xunit;

namespace MergeBot.Test.Integration
{
    public class EndToEndTest : IClassFixture<WebApplicationFactory<MergeBot.Startup>>
    {
        private readonly VCR _vcr;
        private readonly WebApplicationFactory<Startup> _factory;

        public EndToEndTest(WebApplicationFactory<MergeBot.Startup> factory)
        {
            //the directory created should be included in source 
            //control to ensure future runs are playbacked and not recorded.
            var dirInfo = new System.IO.DirectoryInfo("../../../Cassettes"); //3 levels up to get to the root of the test project
            _vcr = new VCR(new FileSystemCassetteStorage(dirInfo));

            _factory = factory.WithWebHostBuilder(c => c.ConfigureTestServices(services => {
                var vcrHandler = _vcr.GetVcrHandler();
                vcrHandler.InnerHandler = new SocketsHttpHandler { UseCookies = false };
                var httpClient = new HttpClient(vcrHandler);
                services.AddSingleton<HttpClient>(httpClient);
            }));
        }

        [Fact]
        public async Task ProcessGitPushSuccessfully()
        {
            using(_vcr.UseCassette("end_to_end", RecordMode.None))
            {
                //arrange
                var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

                ///
                /// Generate JWT
                ///

                //act
                var response = await client.PostAsync("/jwt", new StringContent("integration"));

                //assert
                response.EnsureSuccessStatusCode();
                var jwt = await response.Content.ReadAsStringAsync();
                Assert.NotNull(jwt);


                ///
                /// Invoke webhook
                ///

                //arrange
                var request = new HttpRequestMessage(HttpMethod.Post, "/webhook");
                request.Content = new StringContent(TestHelpers.GetEndToEndGitPushPayload(), Encoding.UTF8, "application/json");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

                //act
                response = await client.SendAsync(request);

                //assert
                response.EnsureSuccessStatusCode();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace MergeBot.Test
{
    public class MergePolicyRunnerFactoryTest
    {
        [Fact]
        public async Task CreatesAndCachesRunnerWhenConfigIsCorrect()
        {
            //arrange
            var repositoryId = "repo-id";
            var extensionId = "ext-id";
            var publisherId = "pub-id";
            var organization = "org";
            ServiceProvider serviceProvider = CreateServiceProvider(extensionId, publisherId);
            HttpClient httpClient = CreateHttpMock(repositoryId, extensionId, publisherId, organization);

            var settings = serviceProvider.GetService<IOptionsMonitor<ExtensionSettings>>();
            var azDoClient = new AzureDevOpsClient(httpClient, "token", settings);

            var subject = new MergePolicyRunnerFactory(
                serviceProvider,
                serviceProvider.GetService<ILogger<MergePolicyRunnerFactory>>()
            );

            var context = new MergePolicyRunnerFactoryContext(azDoClient, repositoryId, organization);

            //act
            var result1 = await subject.CreateAsync(context);
            var result2 = await subject.CreateAsync(context);

            //assert
            Assert.IsType<MergePolicyRunner>(result1);
            Assert.Same(result1, result2);
        }

        [Fact]
        public async Task CreatesNewRunnerWhenConfigurationChanges()
        {
            //arrange
            var repositoryId = "repo-id";
            var extensionId = "ext-id";
            var publisherId = "pub-id";
            var organization = "org";
            ServiceProvider serviceProvider = CreateServiceProvider(extensionId, publisherId);
            HttpClient httpClient = CreateHttpMock(repositoryId, extensionId, publisherId, organization);

            var settings = serviceProvider.GetService<IOptionsMonitor<ExtensionSettings>>();
            var azDoClient = new AzureDevOpsClient(httpClient, "token", settings);

            var subject = new MergePolicyRunnerFactory(
                serviceProvider,
                serviceProvider.GetService<ILogger<MergePolicyRunnerFactory>>()
            );

            var context = new MergePolicyRunnerFactoryContext(azDoClient, repositoryId, organization);

            //act
            var result1 = await subject.CreateAsync(context);
            subject.Clear(new MergePolicyRunnerFactoryContext(context.RepositoryId, context.Organization));
            var result2 = await subject.CreateAsync(context);

            //assert
            Assert.IsType<MergePolicyRunner>(result1);
            Assert.IsType<MergePolicyRunner>(result2);
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public async Task CreatesNoopRunnerWhenNoPoliciesFound()
        {
            //arrange
            var repositoryId = "repo-id";
            var extensionId = "ext-id";
            var publisherId = "pub-id";
            var organization = "org";
            ServiceProvider serviceProvider = CreateServiceProvider(extensionId, publisherId);
            HttpClient httpClient = CreateHttpMockWith404Response(repositoryId, extensionId, publisherId, organization);

            var settings = serviceProvider.GetService<IOptionsMonitor<ExtensionSettings>>();
            var azDoClient = new AzureDevOpsClient(httpClient, "token", settings);

            var subject = new MergePolicyRunnerFactory(
                serviceProvider,
                serviceProvider.GetService<ILogger<MergePolicyRunnerFactory>>()
            );

            var context = new MergePolicyRunnerFactoryContext(azDoClient, repositoryId, organization);

            //act
            var result = await subject.CreateAsync(context);

            //assert
            Assert.IsType<NoopMergePolicyRunner>(result);
        }

        [Fact]
        public async Task CreatesNoopRunnerWhenConfigureMethodThrows()
        {
            //arrange
            var repositoryId = "repo-id";
            var extensionId = "ext-id";
            var publisherId = "pub-id";
            var organization = "org";
            ServiceProvider serviceProvider = CreateServiceProvider(extensionId, publisherId);
            HttpClient httpClient = CreateHttpMockWithInvalidPolicy(repositoryId, extensionId, publisherId, organization);

            var settings = serviceProvider.GetService<IOptionsMonitor<ExtensionSettings>>();
            var azDoClient = new AzureDevOpsClient(httpClient, "token", settings);

            var subject = new MergePolicyRunnerFactory(
                serviceProvider,
                serviceProvider.GetService<ILogger<MergePolicyRunnerFactory>>()
            );

            var context = new MergePolicyRunnerFactoryContext(azDoClient, repositoryId, organization);

            //act
            var result = await subject.CreateAsync(context);

            //assert
            Assert.IsType<NoopMergePolicyRunner>(result);
        }


        private static HttpClient CreateHttpMock(string repositoryId, string extensionId, string publisherId, string organization)
        {
            var url = $"https://extmgmt.dev.azure.com/{organization}/_apis/ExtensionManagement/InstalledExtensions/{publisherId}/{extensionId}/Data/Scopes/Default/Current/Collections/MergePolicies-{Uri.EscapeDataString(repositoryId)}/Documents";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(url).Respond("application/json", File.ReadAllText("Data/merge_policies_valid_response.json"));
            mockHttp.Fallback.Throw(new Exception("No matches"));
            return mockHttp.ToHttpClient();
        }

        private static HttpClient CreateHttpMockWithInvalidPolicy(string repositoryId, string extensionId, string publisherId, string organization)
        {
            var url = $"https://extmgmt.dev.azure.com/{organization}/_apis/ExtensionManagement/InstalledExtensions/{publisherId}/{extensionId}/Data/Scopes/Default/Current/Collections/MergePolicies-{Uri.EscapeDataString(repositoryId)}/Documents";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(url).Respond("application/json", File.ReadAllText("Data/merge_policies_missing_target_response.json"));
            mockHttp.Fallback.Throw(new Exception("No matches"));
            return mockHttp.ToHttpClient();
        }

        private static HttpClient CreateHttpMockWith404Response(string repositoryId, string extensionId, string publisherId, string organization)
        {
            var url = $"https://extmgmt.dev.azure.com/{organization}/_apis/ExtensionManagement/InstalledExtensions/{publisherId}/{extensionId}/Data/Scopes/Default/Current/Collections/MergePolicies-{Uri.EscapeDataString(repositoryId)}/Documents";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(url).Respond(HttpStatusCode.NotFound);
            mockHttp.Fallback.Throw(new Exception("No matches"));
            return mockHttp.ToHttpClient();
        }

        private static ServiceProvider CreateServiceProvider(string extensionId, string publisherId)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ExtensionId"] = extensionId,
                    ["PublisherId"] = publisherId
                })
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddOptions()
                .AddLogging()
                .Configure<ExtensionSettings>(configuration)
                .AddSingleton(Mock.Of<IPullRequestMonitor>())
                .AddTransient<ReleaseBranchCascadingPolicy>()
                .AddTransient<SpecificSourceAndTargetPolicy>()
                .BuildServiceProvider();
            return serviceProvider;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.Options;

namespace MergeBot
{
    public interface IAzureDevOpsClientFactory
    {
        IAzureDevOpsClient Create(string personalAccessToken);
    }

    public sealed class AzureDevOpsClientFactory : IAzureDevOpsClientFactory
    {
        private readonly ConcurrentDictionary<string, IAzureDevOpsClient> _clients = new ConcurrentDictionary<string, IAzureDevOpsClient>();
        private readonly Func<string, IAzureDevOpsClient> _clientFactory;

        public AzureDevOpsClientFactory(HttpClient httpClient, IOptionsMonitor<ExtensionSettings> settings)
        {
            _clientFactory = (personalAccessToken) => new AzureDevOpsClient(httpClient, personalAccessToken, settings);
        }

        public IAzureDevOpsClient Create(string personalAccessToken)
        {
            return _clients.GetOrAdd(personalAccessToken, _clientFactory);
        }
    }
}

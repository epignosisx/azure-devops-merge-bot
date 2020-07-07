using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace MergeBot
{
    public class AzureDevOpsClient : IAzureDevOpsClient
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions { 
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<ExtensionSettings> _settings;
        private readonly AuthenticationHeaderValue _authHeaderValue;

        public AzureDevOpsClient(HttpClient httpClient, string token, IOptionsMonitor<ExtensionSettings> settings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _authHeaderValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", token))));
        }

        public async Task<GitRefsList> GetRefsAsync(string repoUrl)
        {
            var url = $"{repoUrl}/refs?api-version=5.1&filter=heads/";
            var refs = await GetAsync<GitRefsList>(url);
            return refs;
        }

        public async Task<GitPullRequestList> GetOpenPullRequestsAsync(string repoUrl, string sourceBranch, string targetBranch)
        {
            var url = $"{repoUrl}/pullrequests?api-version=5.1&searchCriteria.sourceRefName={Uri.EscapeDataString(sourceBranch)}&searchCriteria.targetRefName={Uri.EscapeDataString(targetBranch)}&searchCriteria.status=active";
            var prs = await GetAsync<GitPullRequestList>(url);
            return prs;
        }

        public async Task<GitPullRequest> GetPullRequestAsync(string repoUrl, int pullRequestId)
        {
            var url = $"{repoUrl}/pullrequests/{pullRequestId}?api-version=5.1";
            var pr = await GetAsync<GitPullRequest>(url);
            return pr;
        }

        public async Task<MergePolicyConfigurationList> GetMergePoliciesAsync(string organization, string repo)
        {
            var settings = _settings.CurrentValue;
            var publisherId = Uri.EscapeDataString(settings.PublisherId);
            var extensionId = Uri.EscapeDataString(settings.ExtensionId);
            var url = $"https://extmgmt.dev.azure.com/{Uri.EscapeDataString(organization)}/_apis/ExtensionManagement/InstalledExtensions/{publisherId}/{extensionId}/Data/Scopes/Default/Current/Collections/MergePolicies-{Uri.EscapeDataString(repo)}/Documents";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = _authHeaderValue;
            using var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new MergePolicyConfigurationList { Value = new System.Collections.Generic.List<MergePolicyConfiguration>(0) };

            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<MergePolicyConfigurationList>(stream, s_serializerOptions);
            return data;
        }

        private async Task<T> GetAsync<T>(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = _authHeaderValue;
            using var response = await _httpClient.SendAsync(request);
            //var content = response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<T>(stream, s_serializerOptions);
            return data;
        }

        public async Task<GitPullRequest> CreatePullRequestAsync(string repoUrl, string source, string target)
        {
            var url = $"{repoUrl}/pullrequests?api-version=5.1";
            var body = new
            {
                sourceRefName = source,
                targetRefName = target,
                title = $"Automatic PR from {source.Replace("refs/heads/", "")} to {target.Replace("refs/heads/", "")}",
                description = "Created by Merge-a-Bot"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = _authHeaderValue;
            var json = JsonSerializer.Serialize(body, body.GetType(), s_serializerOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<GitPullRequest>(stream, s_serializerOptions);
            return data;
        }

        public async Task<GitPullRequest> CompletePullRequestAsync(string repoUrl, int pullRequestId, string lastMergeSourceCommit)
        {
            var url = $"{repoUrl}/pullrequests/{pullRequestId}?api-version=5.1";
            var request = new HttpRequestMessage(HttpMethod.Patch, url);
            request.Headers.Authorization = _authHeaderValue;

            var body = new GitPullRequest
            { 
                Status = "completed",
                LastMergeSourceCommit = new GitCommitRef { 
                    CommitId = lastMergeSourceCommit
                },
                CompletionOptions = new GitPullRequestCompletionOptions { 
                    BypassPolicy = true
                }
            };
            var json = JsonSerializer.Serialize(body, body.GetType(), s_serializerOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<GitPullRequest>(stream, s_serializerOptions);
            return data;
        }
    }
}


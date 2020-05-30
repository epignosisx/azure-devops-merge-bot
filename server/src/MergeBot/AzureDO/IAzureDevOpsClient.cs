using System.Threading.Tasks;

namespace MergeBot
{
    public interface IAzureDevOpsClient
    {
        Task<GitPullRequest> CreatePullRequestAsync(string repoUrl, string source, string target);
        Task<GitRefsList> GetRefsAsync(string repoUrl);
        Task<GitPullRequestList> GetOpenPullRequestsAsync(string repoUrl, string sourceBranch, string targetBranch);
        Task<GitPullRequest> GetPullRequestAsync(string repoUrl, int pullRequestId);
        Task<GitPullRequest> CompletePullRequestAsync(string repoUrl, int pullRequestId, string lastMergeSourceCommit);
        Task<MergePolicyConfigurationList> GetMergePoliciesAsync(string organization, string repo);
    }
}

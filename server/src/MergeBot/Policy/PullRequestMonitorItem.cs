using System;

namespace MergeBot
{
    public sealed class PullRequestMonitorItem
    {
        public PullRequestMonitorItem(IAzureDevOpsClient azDoClient, GitPullRequest pullRequest)
        {
            AzDoClient = azDoClient ?? throw new ArgumentNullException(nameof(azDoClient));
            PullRequest = pullRequest ?? throw new ArgumentNullException(nameof(pullRequest));
        }

        public IAzureDevOpsClient AzDoClient { get; }
        public GitPullRequest PullRequest { get; }
    }
}

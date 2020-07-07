using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MergeBot
{
    public class ReleaseBranchCascadingPolicy : MergePolicy
    {
        public const string PolicyName = "ReleaseBranchCascadingPolicy";

        private readonly ILogger<ReleaseBranchCascadingPolicy> _logger;
        private string? _defaultBranch;
        private ReleaseAndDefaultBranchComparer? _branchComparer;
        private string? _repositoryId;

        public ReleaseBranchCascadingPolicy(
            ILogger<ReleaseBranchCascadingPolicy> logger, 
            IPullRequestMonitor pullRequestGateCompletionMonitor) : base(pullRequestGateCompletionMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override string Name => PolicyName;

        public override void Configure(MergePolicyConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.Target))
                throw new ArgumentException("Target branch cannot be null");

            _defaultBranch = GitBranch.Canonize(configuration.Target);
            _branchComparer = new ReleaseAndDefaultBranchComparer(_defaultBranch);
            _repositoryId = configuration.RepositoryId;
        }

        public override async Task HandleAsync(MergePolicyContext context)
        {
            var update = context.Update;
            var azDoClient = context.AzDoClient;
            var payload = context.Payload;

            if (!string.IsNullOrEmpty(_repositoryId) && !string.Equals(_repositoryId, payload.Resource.Repository.Id))
            {
                _logger.LogDebug(new EventId(5, "SkippingRepoMismatch"), "Skipping {Repository}, does not match {PolicyRepositoryId}", payload.Resource.Repository.Id, _repositoryId);
                return;
            }

            if (!GitBranch.IsRelease(update.Name))
            {
                _logger.LogDebug(new EventId(1, "SkippingNonReleaseBranch"), "Skipping {BranchName} as it is not a release branch", update.Name);
                return;
            }

            var refs = await azDoClient.GetRefsAsync(payload.Resource.Repository.NormalizedUrl);
            var branches = refs.Value
                .Where(n => GitBranch.IsReleaseOrDefault(n.Name, _defaultBranch!))
                .Select(n => new GitBranch(n))
                .OrderBy(n => n, _branchComparer)
                .ToList();

            var sourceBranchIndex = branches.FindIndex(n => n.Name == update.Name);
            if (branches.Count <= sourceBranchIndex + 1)
            {
                _logger.LogDebug(new EventId(2, "TargetBranchNotFound"), "Target branch was not found for {RepositoryId} and {BranchName}", payload.Resource.Repository.Id, update.Name);
                return;
            }

            var sourceBranch = branches[sourceBranchIndex];
            var targetBranch = branches[sourceBranchIndex + 1];

            var openPullRequests = await azDoClient.GetOpenPullRequestsAsync(payload.Resource.Repository.NormalizedUrl, sourceBranch.Name, targetBranch.Name);
            if (openPullRequests.Count > 0)
            {
                _logger.LogDebug(new EventId(3, "PullRequestAlreadyOpen"), "Pull request already open for {RepositoryId} from {SourceBranch} to {TargetBranch}", payload.Resource.Repository.Id, sourceBranch.Name, targetBranch.Name);
                return;
            }

            _logger.LogInformation(new EventId(1, "CreatingPullRequest"), "Creating pull request for {RepositoryId} from {SourceBranch} to {TargetBranch}", payload.Resource.Repository.Id, sourceBranch.Name, targetBranch.Name);
            var pullRequest = await azDoClient.CreatePullRequestAsync(payload.Resource.Repository.NormalizedUrl, sourceBranch.Ref.Name, targetBranch.Ref.Name);
            _logger.LogInformation(new EventId(1, "CreatedPullRequest"), "Created pull request {PullRequestId} for {RepositoryId} from {SourceBranch} to {TargetBranch}", pullRequest.PullRequestId, payload.Resource.Repository.Id, sourceBranch.Name, targetBranch.Name);

            _pullRequestGateCompletionMonitor.Monitor(new PullRequestMonitorItem(azDoClient, pullRequest));
        }
    }
}

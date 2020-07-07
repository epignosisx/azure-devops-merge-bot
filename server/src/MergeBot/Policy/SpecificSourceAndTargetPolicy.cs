using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MergeBot
{
    public class SpecificSourceAndTargetPolicy : MergePolicy
    {
        public const string PolicyName = "SpecificSourceAndTargetPolicy";

        private readonly ILogger<SpecificSourceAndTargetPolicy> _logger;
        private string? _repositoryId;
        private string? _sourceBranch;
        private string? _targetBranch;

        public SpecificSourceAndTargetPolicy(
            ILogger<SpecificSourceAndTargetPolicy> logger,
            IPullRequestMonitor pullRequestGateCompletionMonitor) : base(pullRequestGateCompletionMonitor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override string Name => PolicyName;

        public override void Configure(MergePolicyConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.Source))
                throw new ArgumentException("Source cannot be empty");

            if (string.IsNullOrEmpty(configuration.Target))
                throw new ArgumentException("Target cannot be empty");

            _repositoryId = configuration.RepositoryId;
            _sourceBranch = GitBranch.Canonize(configuration.Source);
            _targetBranch = GitBranch.Canonize(configuration.Target);
        }

        public override async Task HandleAsync(MergePolicyContext context)
        {
            var update = context.Update;
            var payload = context.Payload;
            var azDoClient = context.AzDoClient;
            var sourceBranch = _sourceBranch!;
            var targetBranch = _targetBranch!;
            
            if (!string.IsNullOrEmpty(_repositoryId) && !string.Equals(_repositoryId, payload.Resource.Repository.Id))
            {
                _logger.LogDebug(new EventId(5, "SkippingRepoMismatch"), "Skipping {Repository}, does not match {PolicyRepositoryId}", payload.Resource.Repository.Id, _repositoryId);
                return;
            }

            if (!GitBranch.IsEqual(update.Name, sourceBranch!))
            {
                _logger.LogDebug(new EventId(1, "SkippingNonSourceBranch"), "Skipping {BranchName} as it is not does not match {SourceBranch}", update.Name, sourceBranch);
                return;
            }

            var openPullRequests = await azDoClient.GetOpenPullRequestsAsync(payload.Resource.Repository.NormalizedUrl, sourceBranch, targetBranch);
            if (openPullRequests.Count > 0)
            {
                _logger.LogDebug(new EventId(3, "PullRequestAlreadyOpen"), "Pull request already open for {RepositoryId} from {SourceBranch} to {TargetBranch}", payload.Resource.Repository.Id, sourceBranch, targetBranch);
                return;
            }

            _logger.LogInformation(new EventId(2, "CreatingPullRequest"), "Creating pull request for {RepositoryId} from {SourceBranch} to {TargetBranch}", payload.Resource.Repository.Id, sourceBranch, targetBranch);
            var pullRequest = await azDoClient.CreatePullRequestAsync(payload.Resource.Repository.NormalizedUrl, sourceBranch, targetBranch);
            _logger.LogInformation(new EventId(4, "CreatedPullRequest"), "Created pull request {PullRequestId} for {RepositoryId} from {SourceBranch} to {TargetBranch}", pullRequest.PullRequestId, payload.Resource.Repository.Id, sourceBranch, targetBranch);

            _pullRequestGateCompletionMonitor.Monitor(new PullRequestMonitorItem(azDoClient, pullRequest));
        }
    }

}

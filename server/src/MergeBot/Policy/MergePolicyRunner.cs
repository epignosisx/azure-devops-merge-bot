using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MergeBot
{
    public sealed class MergePolicyRunner : IMergePolicyRunner
    {
        private readonly MergePolicy[] _policies;
        private readonly ILogger<MergePolicyRunner> _logger;

        public MergePolicyRunner(MergePolicy[] policies, ILogger<MergePolicyRunner> logger)
        {
            _policies = policies ?? throw new ArgumentNullException(nameof(policies));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync(IAzureDevOpsClient azDoclient, GitPushEventPayload payload)
        {
            _logger.LogInformation(new EventId(1, "RunningPolicies"), "Running policies for {RepositoryId} with {UpdateCount}", payload.Resource.Repository.Id, payload.Resource.RefUpdates.Count);

            //run policies serially to avoid race conditions in AzDO like duplicate pull request creation.
            foreach (var policy in _policies)
            {
                _logger.LogDebug(new EventId(2, "RunningPolicy"), "Running {Policy}", policy.Name);
                foreach (var update in payload.Resource.RefUpdates)
                {
                    if (IsNewBranch(update.OldObjectId))
                    {
                        _logger.LogInformation(new EventId(2, "SkippingNewBranch"), "Skipping new branch push");
                        continue;
                    }

                    var context = new MergePolicyContext(azDoclient, update, payload);
                    await policy.HandleAsync(context);
                }
            }
        }

        private static bool IsNewBranch(string objectId)
        {
            foreach(var ch in objectId)
            {
                if (ch != '0')
                    return false;
            }
            return true;
        }
    }
}

using System.Threading.Tasks;

namespace MergeBot
{
    public abstract class MergePolicy
    {
        protected readonly IPullRequestMonitor _pullRequestGateCompletionMonitor;

        public MergePolicy(IPullRequestMonitor pullRequestGateCompletionMonitor)
        {
            _pullRequestGateCompletionMonitor = pullRequestGateCompletionMonitor ?? throw new System.ArgumentNullException(nameof(pullRequestGateCompletionMonitor));
        }

        public abstract string Name { get; }

        public abstract void Configure(MergePolicyConfiguration configuration);

        public abstract Task HandleAsync(MergePolicyContext context);
    }
}

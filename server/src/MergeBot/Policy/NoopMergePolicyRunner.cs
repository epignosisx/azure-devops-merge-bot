using System.Threading.Tasks;


namespace MergeBot
{
    public sealed class NoopMergePolicyRunner : IMergePolicyRunner
    {
        public static readonly NoopMergePolicyRunner Instance = new NoopMergePolicyRunner();

        private NoopMergePolicyRunner() { }

        public Task RunAsync(IAzureDevOpsClient azDoclient, GitPushEventPayload payload)
        {
            return Task.CompletedTask;
        }
    }
}

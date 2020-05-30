namespace MergeBot
{
    public sealed class MergePolicyContext
    {
        public MergePolicyContext(IAzureDevOpsClient azDoClient, GitPushEventRefUpdate update, GitPushEventPayload payload)
        {
            AzDoClient = azDoClient;
            Update = update;
            Payload = payload;
        }

        public IAzureDevOpsClient AzDoClient { get; }
        public GitPushEventRefUpdate Update { get; }
        public GitPushEventPayload Payload { get; }
    }
}

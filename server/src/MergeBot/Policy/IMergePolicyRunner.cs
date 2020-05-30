using System.Threading.Tasks;


namespace MergeBot
{
    public interface IMergePolicyRunner
    {
        Task RunAsync(IAzureDevOpsClient azDoclient, GitPushEventPayload payload);
    }
}

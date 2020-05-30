namespace MergeBot
{
    public interface IPullRequestMonitor
    {
        void Monitor(PullRequestMonitorItem item);
    }
}

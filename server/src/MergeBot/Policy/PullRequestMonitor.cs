using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MergeBot
{
    public sealed class PullRequestMonitor : IPullRequestMonitor, IDisposable
    {
        private readonly ConcurrentQueue<PullRequestMonitorItem> _queue = new ConcurrentQueue<PullRequestMonitorItem>();
        private readonly Timer _timer;
        private readonly ILogger<PullRequestMonitor> _logger;
        private int _isRunning = 0;

        public PullRequestMonitor(ILogger<PullRequestMonitor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timer = new Timer(TimerTick, null, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));
        }

        public void Monitor(PullRequestMonitorItem item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            _queue.Enqueue(item);
        }

        private void TimerTick(object? context)
        {
            if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
                return;

            _ = DoWorkAsync();
        }

        private async Task DoWorkAsync()
        {
            _logger.LogTrace(new EventId(4, "NewTick"), "New timer run with {QueueSize}", _queue.Count);
            var requeue = new List<PullRequestMonitorItem>();
            while (_queue.TryDequeue(out PullRequestMonitorItem? item))
            {
                try
                {
                    var processed = await ProcessPullRequestAsync(item);
                    if (!processed)
                        requeue.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(1, "PullRequestProcessFailed"), ex, "Failed to process {PullRequest} for {Repository}", item.PullRequest.PullRequestId, item.PullRequest.Repository.Id);
                }
            }

            foreach (var item in requeue)
            {
                _queue.Enqueue(item);
            }

            Interlocked.Exchange(ref _isRunning, 0);
        }

        private async Task<bool> ProcessPullRequestAsync(PullRequestMonitorItem item)
        {
            var azDoClient = item.AzDoClient;
            var pr = await azDoClient.GetPullRequestAsync(item.PullRequest.Repository.Url, item.PullRequest.PullRequestId);
            if (pr.Status != "active")
            {
                //PR was already completed or abandoned.
                _logger.LogTrace(new EventId(5, "NotActive"), "{PullRequestId} for {Repository} not active", pr.PullRequestId, pr.Repository.Id);
                return true;
            }

            //now we are just dealing with Status = Active

            if (pr.MergeStatus == "queued")
            {
                _logger.LogTrace(new EventId(6, "StillQueued"), "{PullRequestId} for {Repository} is still queued", pr.PullRequestId, pr.Repository.Id);
                return false;
            }

            if (pr.MergeStatus == "succeeded")
            {
                _logger.LogInformation(new EventId(2, "CompletingPullRequest"), "Completing {PullRequestId} for {Repository}", pr.PullRequestId, pr.Repository.Id);
                await azDoClient.CompletePullRequestAsync(pr.Repository.Url, pr.PullRequestId, pr.LastMergeSourceCommit.CommitId);
                _logger.LogInformation(new EventId(3, "CompletedPullRequest"), "Completed {PullRequestId} for {Repository}", pr.PullRequestId, pr.Repository.Id);
            }

            return true;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}

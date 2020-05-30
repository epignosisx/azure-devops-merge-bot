#nullable disable
using System;
using System.Collections.Generic;

namespace MergeBot
{
    public class GitStatsList
    {
        public int Count { get; set; }
        public List<GitStats> Value { get; set; }
    }

    public class GitStats
    {
        /// <summary>
        /// Branch name
        /// </summary>
        public string Name { get; set; }
        public int AheadCount { get; set; }
        public int BehindCount { get; set; }
        public bool IsBaseVersion { get; set; }
    }

    public class GitRefsList
    {
        public int Count { get; set; }
        public List<GitRef> Value { get; set; }
    }

    public class GitRef
    {
        public string Name { get; set; }
        public string ObjectId { get; set; }
        public string Id { get; set; }
    }

    public class GitPullRequestList
    {
        public int Count { get; set; }
        public List<GitPullRequest> Value { get; set; }
    }

    public class GitPullRequest
    {
        public GitRepo Repository { get; set; }
        public int PullRequestId { get; set; }
        public int CodeReviewId { get; set; }
        public string Status { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MergeStatus { get; set; }
        public GitCommitRef LastMergeSourceCommit { get; set; }
    }

    public class GitCommitRef
    {
        public string CommitId { get; set; }
        public string Url { get; set; }
    }

    public class GitRepo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class MergePolicyConfigurationList
    {
        public List<MergePolicyConfiguration> Value { get; set; }
    }

    public class MergePolicyConfiguration
    {
        public DateTime CreateDate { get; set; }
        public string RepositoryId { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public string Strategy { get; set; }
    }
}

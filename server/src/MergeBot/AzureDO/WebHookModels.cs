#nullable disable
using System.Collections.Generic;

namespace MergeBot
{
    public class GitPushEventPayload
    {
        public string EventType { get; set; }
        public GitPushEventResource Resource { get; set; }
    }

    public class GitPushEventResource
    {
        public const string EventType = "git.push";
        public List<GitPushEventRefUpdate> RefUpdates { get; set; }
        public GitPushEventRepository Repository { get; set; }
    }

    public class GitPushEventRefUpdate
    {
        /// <summary>
        /// Branch name: refs/heads/master
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Old commit id: 4f8b247adf567f2b38c512ef52bb03ac0e31ccd4
        /// </summary>
        public string OldObjectId { get; set; }

        /// <summary>
        /// New commit id: 16cec1595902a8a61632e7b9d886a72f332926f3
        /// </summary>
        public string NewObjectId { get; set; }
    }

    public class GitPushEventRepository
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }
}

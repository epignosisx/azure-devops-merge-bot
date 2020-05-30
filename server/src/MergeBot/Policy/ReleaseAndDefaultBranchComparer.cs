using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MergeBot
{
    public class ReleaseAndDefaultBranchComparer : IComparer<GitBranch>
    {
        private readonly string _defaultBranch;

        public ReleaseAndDefaultBranchComparer(string defaultBranch) => _defaultBranch = defaultBranch;

        public int Compare([AllowNull] GitBranch x, [AllowNull] GitBranch y)
        {
            if (x is null)
                return -1;

            if (y is null)
                return 1;

            if (GitBranch.IsEqual(_defaultBranch, x.Name))
                return 1;

            if (GitBranch.IsEqual(_defaultBranch, y.Name))
                return -1;

            return x.Semver!.CompareByPrecedence(y.Semver);
        }
    }

}

using System;
using Semver;

namespace MergeBot
{
    public class GitBranch
    {
        public string Name => Ref.Name;
        public SemVersion? Semver { get; }
        public GitRef Ref { get; }

        public GitBranch(GitRef gitRef)
        {
            Ref = gitRef;
            if (IsRelease(gitRef.Name))
            {
                string version = gitRef.Name.Replace("refs/heads/release/", "");
                SemVersion.TryParse(version, out SemVersion semver, strict: false);
                Semver = semver;
            }
        }

        public static bool IsRelease(string branchName) => branchName.StartsWith("refs/heads/release/", StringComparison.Ordinal);

        public static bool IsEqual(string branchName, string otherBranchName) => string.Equals(branchName, otherBranchName, StringComparison.Ordinal);

        public static bool IsReleaseOrDefault(string branchName, string defaultBranch) => IsRelease(branchName) || IsEqual(branchName, defaultBranch);

        public static string Canonize(string branchName)
        {
            if (branchName.StartsWith("refs/heads/", StringComparison.Ordinal))
                return branchName;

            return "refs/heads/" + branchName;
        }
    }
}

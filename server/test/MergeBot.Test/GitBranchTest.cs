using System.Linq;
using Xunit;

namespace MergeBot.Test
{
    public class GitBranchTest
    {
        [Fact]
        public void CreatesSemverForReleaseBranch()
        {
            //arrange
            var gitRef = new GitRef { Id = "", Name = "refs/heads/release/1.0", ObjectId = "" };

            //act
            var result = new GitBranch(gitRef);

            //assert
            Assert.NotNull(result.Semver);
        }

        [Theory]
        [InlineData("refs/heads/master")]
        [InlineData("refs/heads/develop")]
        public void SortsReleaseAndDefaultBranches(string defaultBranch)
        {
            //arrange
            var branches = new[] {
                new GitBranch(new GitRef { Name = defaultBranch }),
                new GitBranch(new GitRef { Name = "refs/heads/release/1.0" }),
                new GitBranch(new GitRef { Name = "refs/heads/release/0.0" }),
                new GitBranch(new GitRef { Name = "refs/heads/release/2.0" }),
                new GitBranch(new GitRef { Name = "refs/heads/release/1.1" }),
            };
            var comparer = new ReleaseAndDefaultBranchComparer(defaultBranch);

            //act
            var result = branches.OrderBy(n => n, comparer).ToArray();

            //assert
            Assert.Equal("refs/heads/release/0.0", result[0].Name);
            Assert.Equal("refs/heads/release/1.0", result[1].Name);
            Assert.Equal("refs/heads/release/1.1", result[2].Name);
            Assert.Equal("refs/heads/release/2.0", result[3].Name);
            Assert.Equal(defaultBranch, result[4].Name);
        }

        [Theory]
        [InlineData("refs/heads/master")]
        [InlineData("refs/heads/develop")]
        public void SortsReleaseAndDefaultBranchesUsingYearMonthDay(string defaultBranch)
        {
            //arrange
            var branches = new[] {
                new GitBranch(new GitRef { Name = defaultBranch }),
                new GitBranch(new GitRef { Name = "refs/heads/release/2020.1.19" }),
                new GitBranch(new GitRef { Name = "refs/heads/release/2020.01.20" }),
                new GitBranch(new GitRef { Name = "refs/heads/release/2020.1.21" }),
                new GitBranch(new GitRef { Name = "refs/heads/release/2020.2.21" }),
            };
            var comparer = new ReleaseAndDefaultBranchComparer(defaultBranch);

            //act
            var result = branches.OrderBy(n => n, comparer).ToArray();

            //assert
            Assert.Equal("refs/heads/release/2020.1.19", result[0].Name);
            Assert.Equal("refs/heads/release/2020.01.20", result[1].Name);
            Assert.Equal("refs/heads/release/2020.1.21", result[2].Name);
            Assert.Equal("refs/heads/release/2020.2.21", result[3].Name);
            Assert.Equal(defaultBranch, result[4].Name);
        }
    }
}

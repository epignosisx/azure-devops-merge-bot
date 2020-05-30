using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MergeBot.Test
{
    public class MergePolicyRunnerTest
    {
        [Fact]
        public async Task DoesNotRunPoliciesForNewlyCreatedBranch()
        {
            //arrange
            var fakePolicy = new FakePolicy(Mock.Of<IPullRequestMonitor>());
            var policies = new[] { fakePolicy };
            var subject = new MergePolicyRunner(policies, Mock.Of<ILogger<MergePolicyRunner>>());
            var payload = new GitPushEventPayload
            {
                Resource = new GitPushEventResource
                {
                    RefUpdates = new List<GitPushEventRefUpdate>
                    {
                        new GitPushEventRefUpdate
                        {
                            Name = "refs/heads/master",
                            OldObjectId = "0000000000000000000000000",
                            NewObjectId = "1231231231231231231231231"
                        }
                    },
                    Repository = new GitPushEventRepository
                    {
                        Name = "RepoName"
                    }
                }
            };

            //act
            await subject.RunAsync(Mock.Of<IAzureDevOpsClient>(), payload);

            //assert
            Assert.False(fakePolicy.WasCalled);
        }

        private class FakePolicy : MergePolicy
        {
            public FakePolicy(IPullRequestMonitor pullRequestGateCompletionMonitor) : base(pullRequestGateCompletionMonitor)
            {
            }

            public bool WasCalled { get; private set; }

            public override string Name => "FakePolicy";

            public override void Configure(MergePolicyConfiguration configuration)
            {
            }

            public override Task HandleAsync(MergePolicyContext context)
            {
                WasCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}

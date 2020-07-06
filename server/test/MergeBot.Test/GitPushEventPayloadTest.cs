using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MergeBot.Test
{
    public class GitPushEventPayloadTest
    {
        [Fact]
        public async Task DeserializesToClass()
        {
            //arrange
            using var fs = TestHelpers.GetGitPushPayloadStream();

            //act
            var payload = await WebhookDeserializer.DeserializeAsync(fs);

            //assert
            Assert.Equal(GitPushEventResource.EventType, payload.EventType);
            Assert.Equal("refs/heads/release/1.0", payload.Resource.RefUpdates[0].Name);
            Assert.NotNull(payload.Resource.Repository.Id);
            Assert.NotNull(payload.Resource.Repository.Url);
        }

        [Theory]
        [InlineData("https://foo.visualstudio.com/_apis/git/repositories/29769056-6417-4bdb-9b10-aab142fbfedf")]
        [InlineData("https://dev.azure.com/foo/_apis/git/repositories/29769056-6417-4bdb-9b10-aab142fbfedf")]
        public void GetOrganization_ExtractsOrganizationFromKnownFormats(string repositoryUrl)
        {
            //arrange
            var repo = new GitPushEventRepository { Url = repositoryUrl };

            //act
            var result = repo.GetOrganization();

            //assert
            Assert.Equal("foo", result);
        }

        [Fact]
        public void GetOrganization_ThrowsWhenUrlFormatIsNotRecognized()
        {
            //arrange
            var repo = new GitPushEventRepository { Url = "https://foo.other-domain.com/_apis/git/repositories/29769056-6417-4bdb-9b10-aab142fbfedf" };

            //act + assert
            Assert.Throws<InvalidOperationException>(() => repo.GetOrganization());
        }
    }
}

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
    }
}

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MergeBot
{
    public static class WebhookDeserializer
    {
        private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static async ValueTask<GitPushEventPayload?> DeserializeAsync(Stream stream)
        {
            //for troubleshooting
            //using var sr = new StreamReader(stream);
            //string content = await sr.ReadToEndAsync();
            var payload = await JsonSerializer.DeserializeAsync<GitPushEventPayload>(stream, s_serializerOptions);
            if (payload.EventType == GitPushEventResource.EventType && payload.Resource?.RefUpdates != null && payload.Resource?.Repository != null)
                return payload;

            return null;
        }
    }

}

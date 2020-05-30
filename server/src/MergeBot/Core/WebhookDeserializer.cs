using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
            var payload = await JsonSerializer.DeserializeAsync<GitPushEventPayload>(stream, s_serializerOptions);
            if (payload.EventType == GitPushEventResource.EventType)
                return payload;

            return null;
        }
    }

}

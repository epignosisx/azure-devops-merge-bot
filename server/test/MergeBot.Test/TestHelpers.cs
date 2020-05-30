using System.IO;

namespace MergeBot.Test
{
    public static class TestHelpers
    {
        public static string GetEndToEndGitPushPayload() => File.ReadAllText("Data/end_to_end_push_event_payload.json");
        public static FileStream GetGitPushPayloadStream() => File.OpenRead("Data/git_push_event_payload.json");
    }
}

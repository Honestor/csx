using System.Text.Json;

namespace Framework.AspNetCore.InstantMessaging.Protocols.Json
{
    public class JsonHubProtocolOptions
    {
        public JsonSerializerOptions PayloadSerializerOptions { get; set; } = JsonHubProtocol.CreateDefaultSerializerSettings();
    }
}

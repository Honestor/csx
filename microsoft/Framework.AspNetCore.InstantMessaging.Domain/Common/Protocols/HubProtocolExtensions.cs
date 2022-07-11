

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    public static class HubProtocolExtensions
    {
        public static byte[] GetMessageBytes(this IHubProtocol hubProtocol, HubMessage message)
        {
            var writer = MemoryBufferWriter.Get();
            try
            {
                hubProtocol.WriteMessage(message, writer);
                return writer.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }
    }
}

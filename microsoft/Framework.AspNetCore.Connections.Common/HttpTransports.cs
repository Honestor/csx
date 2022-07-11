namespace Framework.AspNetCore.Connections.Common
{
    public static class HttpTransports
    {
        public static readonly HttpTransportType All = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
    }
}

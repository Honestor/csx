using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Framework.AspNetCore.Connections.Common
{
    /// <summary>
    /// 协商协议
    /// </summary>
    public static class NegotiateProtocol
    {
        private const string ConnectionIdPropertyName = "connectionId";
        private static JsonEncodedText ConnectionIdPropertyNameBytes = JsonEncodedText.Encode(ConnectionIdPropertyName);
        private const string ConnectionTokenPropertyName = "connectionToken";
        private static JsonEncodedText ConnectionTokenPropertyNameBytes = JsonEncodedText.Encode(ConnectionTokenPropertyName);
        private const string UrlPropertyName = "url";
        private static JsonEncodedText UrlPropertyNameBytes = JsonEncodedText.Encode(UrlPropertyName);
        private const string AccessTokenPropertyName = "accessToken";
        private static JsonEncodedText AccessTokenPropertyNameBytes = JsonEncodedText.Encode(AccessTokenPropertyName);
        private const string AvailableTransportsPropertyName = "availableTransports";
        private static JsonEncodedText AvailableTransportsPropertyNameBytes = JsonEncodedText.Encode(AvailableTransportsPropertyName);
        private const string TransportPropertyName = "transport";
        private static JsonEncodedText TransportPropertyNameBytes = JsonEncodedText.Encode(TransportPropertyName);
        private const string TransferFormatsPropertyName = "transferFormats";
        private static JsonEncodedText TransferFormatsPropertyNameBytes = JsonEncodedText.Encode(TransferFormatsPropertyName);
        private const string ErrorPropertyName = "error";
        private static JsonEncodedText ErrorPropertyNameBytes = JsonEncodedText.Encode(ErrorPropertyName);
        private const string NegotiateVersionPropertyName = "negotiateVersion";
        private static JsonEncodedText NegotiateVersionPropertyNameBytes = JsonEncodedText.Encode(NegotiateVersionPropertyName);

        public static void WriteResponse(NegotiationResponse response, IBufferWriter<byte> output)
        {
            var reusableWriter = ReusableUtf8JsonWriter.Get(output);

            try
            {
                var writer = reusableWriter.GetJsonWriter();
                writer.WriteStartObject();
                if (!string.IsNullOrEmpty(response.Error))
                {
                    writer.WriteString(ErrorPropertyNameBytes, response.Error);
                    writer.WriteEndObject();
                    writer.Flush();
                    Debug.Assert(writer.CurrentDepth == 0);
                    return;
                }

                writer.WriteNumber(NegotiateVersionPropertyNameBytes, response.Version);

                if (!string.IsNullOrEmpty(response.Url))
                {
                    writer.WriteString(UrlPropertyNameBytes, response.Url);
                }

                if (!string.IsNullOrEmpty(response.AccessToken))
                {
                    writer.WriteString(AccessTokenPropertyNameBytes, response.AccessToken);
                }

                if (!string.IsNullOrEmpty(response.ConnectionId))
                {
                    writer.WriteString(ConnectionIdPropertyNameBytes, response.ConnectionId);
                }

                if (response.Version > 0 && !string.IsNullOrEmpty(response.ConnectionToken))
                {
                    writer.WriteString(ConnectionTokenPropertyNameBytes, response.ConnectionToken);
                }

                writer.WriteStartArray(AvailableTransportsPropertyNameBytes);

                if (response.AvailableTransports != null)
                {
                    var transportCount = response.AvailableTransports.Count;
                    for (var i = 0; i < transportCount; ++i)
                    {
                        var availableTransport = response.AvailableTransports[i];
                        writer.WriteStartObject();
                        if (availableTransport.Transport != null)
                        {
                            writer.WriteString(TransportPropertyNameBytes, availableTransport.Transport);
                        }
                        else
                        {
                            writer.WriteNull(TransportPropertyNameBytes);
                        }
                        writer.WriteStartArray(TransferFormatsPropertyNameBytes);

                        if (availableTransport.TransferFormats != null)
                        {
                            var formatCount = availableTransport.TransferFormats.Count;
                            for (var j = 0; j < formatCount; ++j)
                            {
                                writer.WriteStringValue(availableTransport.TransferFormats[j]);
                            }
                        }

                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }
                }

                writer.WriteEndArray();
                writer.WriteEndObject();

                writer.Flush();
                Debug.Assert(writer.CurrentDepth == 0);
            }
            finally
            {
                ReusableUtf8JsonWriter.Return(reusableWriter);
            }
        }
    }
}

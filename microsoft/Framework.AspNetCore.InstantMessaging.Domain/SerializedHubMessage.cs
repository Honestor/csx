using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub消息序列化
    /// </summary>
    public class SerializedHubMessage
    {
        public HubMessage? Message { get; }

        public SerializedHubMessage(HubMessage message)
        {
            Message = message;
        }

        private readonly object _lock = new object();
        public ReadOnlyMemory<byte> GetSerializedMessage(IHubProtocol protocol)
        {
            lock (_lock)
            {
                if (!TryGetCachedUnsynchronized(protocol.Name, out var serialized))
                {
                    if (Message == null)
                    {
                        throw new InvalidOperationException(
                            "This message was received from another server that did not have the requested protocol available.");
                    }

                    serialized = protocol.GetMessageBytes(Message);
                    SetCacheUnsynchronized(protocol.Name, serialized);
                }

                return serialized;
            }
        }

        private SerializedMessage _cachedItem1;
        private SerializedMessage _cachedItem2;
        private List<SerializedMessage>? _cachedItems;
        private bool TryGetCachedUnsynchronized(string protocolName, out ReadOnlyMemory<byte> result)
        {
            if (string.Equals(_cachedItem1.ProtocolName, protocolName, StringComparison.Ordinal))
            {
                result = _cachedItem1.Serialized;
                return true;
            }

            if (string.Equals(_cachedItem2.ProtocolName, protocolName, StringComparison.Ordinal))
            {
                result = _cachedItem2.Serialized;
                return true;
            }

            if (_cachedItems != null)
            {
                foreach (var serializedMessage in _cachedItems)
                {
                    if (string.Equals(serializedMessage.ProtocolName, protocolName, StringComparison.Ordinal))
                    {
                        result = serializedMessage.Serialized;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        private void SetCacheUnsynchronized(string protocolName, ReadOnlyMemory<byte> serialized)
        {
            // We set the fields before moving on to the list, if we need it to hold more than 2 items.
            // We have to read/write these fields under the lock because the structs might tear and another
            // thread might observe them half-assigned

            if (_cachedItem1.ProtocolName == null)
            {
                _cachedItem1 = new SerializedMessage(protocolName, serialized);
            }
            else if (_cachedItem2.ProtocolName == null)
            {
                _cachedItem2 = new SerializedMessage(protocolName, serialized);
            }
            else
            {
                if (_cachedItems == null)
                {
                    _cachedItems = new List<SerializedMessage>();
                }

                foreach (var item in _cachedItems)
                {
                    if (string.Equals(item.ProtocolName, protocolName, StringComparison.Ordinal))
                    {
                        // No need to add
                        return;
                    }
                }

                _cachedItems.Add(new SerializedMessage(protocolName, serialized));
            }
        }
    }
}

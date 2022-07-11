using System;

namespace Framework.AspNetCore.Connections.Abstractions
{
    [Flags]
    public enum TransferFormat
    {
        Binary = 0x01,
        Text = 0x02
    }
}
using System.Collections.Generic;

namespace Framework.AspNetCore.Connections.Common
{
    public class AvailableTransport
    {
        public string Transport { get; set; }

        public IList<string> TransferFormats { get; set; }
    }
}

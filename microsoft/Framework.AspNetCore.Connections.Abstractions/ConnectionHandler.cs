using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections.Abstractions
{
    public abstract class ConnectionHandler
    {
        public abstract Task OnConnectedAsync(ConnectionContext connection);
    }
}

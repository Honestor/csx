using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections.Abstractions
{
    /// <summary>
    /// 连接管道中执行的委托
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public delegate Task ConnectionDelegate(ConnectionContext connection);
}

using System.Threading.Tasks;

namespace Framework.AspNetCore.Connections.Abstractions
{
    /// <summary>
    /// ���ӹܵ���ִ�е�ί��
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public delegate Task ConnectionDelegate(ConnectionContext connection);
}

using System;

namespace Framework.AspNetCore.Connections.Abstractions
{
    public interface IConnectionBuilder
    {
        /// <summary>
        /// DI
        /// </summary>
        IServiceProvider ApplicationServices { get; }

        /// <summary>
        /// ����м��ί�е����ӹܵ�
        /// </summary>
        /// <param name="middleware"></param>
        /// <returns></returns>
        IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);

        /// <summary>
        /// �����м��ί��
        /// </summary>
        /// <returns></returns>
        ConnectionDelegate Build();
    }
}

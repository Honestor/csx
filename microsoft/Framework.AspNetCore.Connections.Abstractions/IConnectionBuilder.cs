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
        /// 添加中间件委托到连接管道
        /// </summary>
        /// <param name="middleware"></param>
        /// <returns></returns>
        IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);

        /// <summary>
        /// 构建中间件委托
        /// </summary>
        /// <returns></returns>
        ConnectionDelegate Build();
    }
}

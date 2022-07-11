using System;

namespace Framework.AspNetCore.InstantMessaging.Domain
{
    /// <summary>
    /// Hub 连接上下文配置
    /// </summary>
    public class HubConnectionContextOptions
    {
        /// <summary>
        /// 心跳检测时间
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; }

        /// <summary>
        /// 在服务器关闭连接之前,客户端必须发送一个消息的超时时间设置
        /// </summary>
        public TimeSpan ClientTimeoutInterval { get; set; }

        /// <summary>
        /// 客户端发送消息的最大值
        /// </summary>
        public long? MaximumReceiveMessageSize { get; set; }

        /// <summary>
        /// 模块时间Provider
        /// </summary>
        internal ISystemClock SystemClock { get; set; } = default!;

        /// <summary>
        /// 当页面的hub 调用hub方法 同一时间发送两条信息,如果值是1则同步执行,如果大于1并发异步执行
        /// </summary>
        public int MaximumParallelInvocations { get; set; } = 1;
    }
}

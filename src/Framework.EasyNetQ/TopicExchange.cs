using EasyNetQ;

namespace Framework.EasyNetQ
{
    public class TopicExchange
    {
        /// <summary>
        /// 是否持久化
        /// </summary>
        public bool Durable { get; set; } = true;

        /// <summary>
        /// 是否自动删除 true,关联队列完成且移除时,交换器会自动删除
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// 交换机名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 消息无法路由到队列的响应机制 true 发送一条无法无法路由的消息,false 则丢弃消息 默认true
        /// </summary>
        public bool Mandatory { get; set; } = true;

        /// <summary>
        /// 路由键
        /// </summary>
        public string RouteKey { get; set; }

        /// <summary>
        /// 消息属性
        /// </summary>
        public MessageProperties MessageProperties { get; set; } = new MessageProperties();
    }
}

using Framework.EasyNetQ;

namespace Framework.Canal.EasyNetQ.Configurations
{
    /// <summary>
    /// 配置
    /// </summary>
    public class CanalToEasyNetQOptions
    {
        public List<Filter> Filters { get; set; } = new List<Filter>();
    }

    public class Filter
    { 
        /// <summary>
        /// 订阅逻辑
        /// </summary>
        public string CanalFilter { get; set; }

        /// <summary>
        /// Topic交换机信息
        /// </summary>
        public TopicExchange TopicExchange { get; set; }
    }
}

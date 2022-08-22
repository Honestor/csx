using Framework.Core.Collections;
using Framework.Timing;
using System.Collections.Generic;

namespace Framework.Json
{
    public class JsonOptions
    {
        /// <summary>
        /// 默认时间序列化格式
        /// </summary>
        public string DefaultDateTimeFormat { get; set; }

        /// <summary>
        /// 额外支持的时间Formats,用于默认的解析不了,如2020-03-25 13:54:57.486928,mysql等一些数据库支持时间配置精度
        /// </summary>
        public List<string> ExtraDateTimeFormats { get; set; }

        /// <summary>
        /// 混合序列化 在api使用system.text.json的基础上集成newton(功能相对较多)
        /// </summary>
        public bool UseHybridSerializer { get; set; } = true;

        /// <summary>
        /// Json序列化 Provider集合暂时只有newton和text.json
        /// </summary>
        public ITypeList<IJsonSerializerProvider> Providers { get; }

        public JsonOptions()
        {
            Providers = new TypeList<IJsonSerializerProvider>();
            DefaultDateTimeFormat = DateTimeFormats.LongTime;
            ExtraDateTimeFormats = new List<string>();
        }
    }
}

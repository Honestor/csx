namespace Framework.AspNetCore.Connections.Abstractions
{
    /// <summary>
    /// 传输内容格式
    /// </summary>
    public interface ITransferFormatFeature
    {
        /// <summary>
        /// 支持的格式
        /// </summary>
        TransferFormat SupportedFormats { get; }

        /// <summary>
        /// 当前使用的格式
        /// </summary>
        TransferFormat ActiveFormat { get; set; }
    }
}
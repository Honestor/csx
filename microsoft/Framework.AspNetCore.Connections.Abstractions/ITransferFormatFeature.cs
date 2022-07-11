namespace Framework.AspNetCore.Connections.Abstractions
{
    /// <summary>
    /// �������ݸ�ʽ
    /// </summary>
    public interface ITransferFormatFeature
    {
        /// <summary>
        /// ֧�ֵĸ�ʽ
        /// </summary>
        TransferFormat SupportedFormats { get; }

        /// <summary>
        /// ��ǰʹ�õĸ�ʽ
        /// </summary>
        TransferFormat ActiveFormat { get; set; }
    }
}
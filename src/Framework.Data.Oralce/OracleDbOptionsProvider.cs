using Framework.Core;
using Framework.Core.Dependency;
using Microsoft.Extensions.Options;

namespace Framework.Data.Oralce
{
    public class OracleDbOptionsProvider:ISingleton
    {
        private OralceDbOptions _oralceDbOptions;
        public OracleDbOptionsProvider(IOptionsMonitor<OralceDbOptions> options)
        {
            _oralceDbOptions = options.CurrentValue;
        }

        public OralceDbOptions Get()
        {
            if (_oralceDbOptions == null)
                throw new FrameworkException($"please set {nameof(OralceDbOptions)} in appsettings.json because oracle module need this options to connect oracle database server");
            return _oralceDbOptions;
        }
    }

}

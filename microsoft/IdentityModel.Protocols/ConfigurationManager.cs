using IdentityModel.Logging;
using IdentityModel.Tokens;
using System;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityModel.Protocols
{
    /// <summary>
    /// Manages the retrieval of Configuration data.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IDocumentRetriever"/>.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class ConfigurationManager<T> : IConfigurationManager<T> where T : class
    {
        private readonly SemaphoreSlim _refreshLock;
        private readonly string _metadataAddress;
        private readonly IDocumentRetriever _docRetriever;
        private readonly IConfigurationRetriever<T> _configRetriever;
        private T _currentConfiguration;
        private DateTimeOffset _syncAfter = DateTimeOffset.MinValue;
        private DateTimeOffset _lastRefresh = DateTimeOffset.MinValue;

        /// <summary>
        /// Instantiaties a new <see cref="ConfigurationManager{T}"/> that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever)
            : this(metadataAddress, configRetriever, new HttpDocumentRetriever())
        {
        }

        /// <summary>
        /// Instantiaties a new <see cref="ConfigurationManager{T}"/> that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        /// <param name="httpClient">The client to use when obtaining configuration.</param>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, HttpClient httpClient)
            : this(metadataAddress, configRetriever, new HttpDocumentRetriever(httpClient))
        {
        }

        /// <summary>
        /// Instantiaties a new <see cref="ConfigurationManager{T}"/> that manages automatic and controls refreshing on configuration data.
        /// </summary>
        /// <param name="metadataAddress">The address to obtain configuration.</param>
        /// <param name="configRetriever">The <see cref="IConfigurationRetriever{T}"/></param>
        /// <param name="docRetriever">The <see cref="IDocumentRetriever"/> that reaches out to obtain the configuration.</param>
        /// <exception cref="ArgumentNullException">If 'metadataAddress' is null or empty.</exception>
        /// <exception cref="ArgumentNullException">If 'configRetriever' is null.</exception>
        /// <exception cref="ArgumentNullException">If 'docRetriever' is null.</exception>
        public ConfigurationManager(string metadataAddress, IConfigurationRetriever<T> configRetriever, IDocumentRetriever docRetriever)
        {
            if (string.IsNullOrWhiteSpace(metadataAddress))
                throw new Exception("");

            if (configRetriever == null)
                throw new Exception("");

            if (docRetriever == null)
                throw new Exception("");

            _metadataAddress = metadataAddress;
            _docRetriever = docRetriever;
            _configRetriever = configRetriever;
            _refreshLock = new SemaphoreSlim(1);
        }

        public async Task<T> GetConfigurationAsync()
        {
            return await GetConfigurationAsync(CancellationToken.None).ConfigureAwait(false);
        }


        public async Task<T> GetConfigurationAsync(CancellationToken cancel)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (_currentConfiguration != null && _syncAfter > now)
            {
                return _currentConfiguration;
            }

            await _refreshLock.WaitAsync(cancel).ConfigureAwait(false);
            try
            {
                if (_syncAfter <= now)
                {
                    try
                    {
                        // Don't use the individual CT here, this is a shared operation that shouldn't be affected by an individual's cancellation.
                        // The transport should have it's own timeouts, etc..
                        _currentConfiguration = await _configRetriever.GetConfigurationAsync(_metadataAddress, _docRetriever, CancellationToken.None).ConfigureAwait(false);
                        Contract.Assert(_currentConfiguration != null);
                        _lastRefresh = now;
                        _syncAfter = DateTimeUtil.Add(now.UtcDateTime, _automaticRefreshInterval);
                    }
                    catch (Exception ex)
                    {
                        _syncAfter = DateTimeUtil.Add(now.UtcDateTime, _automaticRefreshInterval < _refreshInterval ? _automaticRefreshInterval : _refreshInterval);
                        if (_currentConfiguration == null) // Throw an exception if there's no configuration to return.
                            throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(LogMessages.IDX20803, (_metadataAddress ?? "null")), ex));
                        else
                            LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(LogMessages.IDX20806, (_metadataAddress ?? "null")), ex));
                    }
                }

                // Stale metadata is better than no metadata
                if (_currentConfiguration != null)
                    return _currentConfiguration;
                else
                {
                    throw LogHelper.LogExceptionMessage(new InvalidOperationException(LogHelper.FormatInvariant(LogMessages.IDX20803, (_metadataAddress ?? "null"))));
                }
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public void RequestRefresh()
        {
            throw new NotImplementedException();
        }

        public static readonly TimeSpan MinimumRefreshInterval = new TimeSpan(0, 0, 0, 1);

        public static readonly TimeSpan MinimumAutomaticRefreshInterval = new TimeSpan(0, 0, 5, 0);

        public static readonly TimeSpan DefaultRefreshInterval = new TimeSpan(0, 0, 0, 30);
        private TimeSpan _refreshInterval = DefaultRefreshInterval;
        public TimeSpan RefreshInterval
        {
            get { return _refreshInterval; }
            set
            {
                if (value < MinimumRefreshInterval)
                    throw new Exception("");

                _refreshInterval = value;
            }
        }

        public static readonly TimeSpan DefaultAutomaticRefreshInterval = new TimeSpan(1, 0, 0, 0);
        private TimeSpan _automaticRefreshInterval = DefaultAutomaticRefreshInterval;
        public TimeSpan AutomaticRefreshInterval
        {
            get { return _automaticRefreshInterval; }
            set
            {
                if (value < MinimumAutomaticRefreshInterval)
                    throw new Exception("");

                _automaticRefreshInterval = value;
            }
        }
    }
}   

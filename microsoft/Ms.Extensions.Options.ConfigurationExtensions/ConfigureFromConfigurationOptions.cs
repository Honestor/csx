// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Ms.Configuration.Abstracts;
using Ms.Extensions.Configuration;
using System;

namespace Ms.Extensions.Options.ConfigurationExtensions
{
    // REVIEW: consider deleting/obsoleting, not used by Configure anymore (in favor of name), left for breaking change)

    /// <summary>
    /// Configures an option instance by using <see cref="ConfigurationBinder.Bind(IConfiguration, object)"/> against an <see cref="IConfiguration"/>.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to bind.</typeparam>
    public class ConfigureFromConfigurationOptions<TOptions> : ConfigureOptions<TOptions>
        where TOptions : class
    {
        public ConfigureFromConfigurationOptions(IConfiguration config)
            : base(options => BindFromOptions(options, config))
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
        }

        private static void BindFromOptions(TOptions options, IConfiguration config) => ConfigurationBinder.Bind(config, options);
    }
}

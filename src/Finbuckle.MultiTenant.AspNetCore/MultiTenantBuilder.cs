//    Copyright 2018 Andrew White
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Finbuckle.MultiTenant.AspNetCore;
using Finbuckle.MultiTenant.Strategies;
using Finbuckle.MultiTenant.Stores;
using Microsoft.AspNetCore.Routing;

namespace Finbuckle.MultiTenant
{
    /// <summary>
    /// Configures <c>Finbuckle.MultiTenant.AspNetCore</c> services and configuration.
    /// </summary>
    public class MultiTenantBuilder
    {
        internal readonly IServiceCollection services;

        public MultiTenantBuilder(IServiceCollection services)
        {
            this.services = services;
        }

        /// <summary>
        /// Adds per-tenant configuration for an options class. (Obsolete: use <c>WithPerTenantOptions</c> instead).
        /// </summary>
        /// <param name="tenantInfo">The configuration action to be run for each tenant.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        [Obsolete("WithPerTenantOptionsConfig is obsolete. Use WithPerTenantOptions instead.")]
        public MultiTenantBuilder WithPerTenantOptionsConfig<TOptions>(Action<TOptions, TenantInfo> tenantInfo) where TOptions : class
        {
            services.TryAddSingleton<IOptionsMonitorCache<TOptions>>(sp =>
                {
                    return (MultiTenantOptionsCache<TOptions>)
                        ActivatorUtilities.CreateInstance(sp, typeof(MultiTenantOptionsCache<TOptions>), new[] { tenantInfo });
                });

            return this;
        }

        /// <summary>
        /// Adds per-tenant configuration for an options class.
        /// </summary>
        /// <param name="tenantInfo">The configuration action to be run for each tenant.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithPerTenantOptions<TOptions>(Action<TOptions, TenantInfo> tenantInfo) where TOptions : class, new()
        {
            if (tenantInfo == null)
            {
                throw new ArgumentNullException(nameof(tenantInfo));
            }

            // Handles multiplexing cached options.
            services.TryAddSingleton<IOptionsMonitorCache<TOptions>>(sp =>
                {
                    return (MultiTenantOptionsCache<TOptions>)
                        ActivatorUtilities.CreateInstance(sp, typeof(MultiTenantOptionsCache<TOptions>));
                });

            // Necessary to apply tenant options in between configuration and postconfiguration
            services.TryAddTransient<IOptionsFactory<TOptions>>(sp =>
            {
                return (IOptionsFactory<TOptions>)ActivatorUtilities.
                    CreateInstance(sp, typeof(MultiTenantOptionsFactory<TOptions>), new[] { tenantInfo });
            });

            services.TryAddScoped<IOptionsSnapshot<TOptions>>(sp => BuildOptionsManager<TOptions>(sp));

            services.TryAddSingleton<IOptions<TOptions>>(sp => BuildOptionsManager<TOptions>(sp));

            return this;
        }

        private static MultiTenantOptionsManager<TOptions> BuildOptionsManager<TOptions>(IServiceProvider sp) where TOptions : class, new()
        {
            var cache = ActivatorUtilities.CreateInstance(sp, typeof(MultiTenantOptionsCache<TOptions>));
            return (MultiTenantOptionsManager<TOptions>)
                ActivatorUtilities.CreateInstance(sp, typeof(MultiTenantOptionsManager<TOptions>), new[] { cache });
        }

        /// <summary>
        /// Configures support for multitenant OAuth and OpenIdConnect.
        /// </summary>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithRemoteAuthentication()
        {
            // Replace needed instead of TryAdd...
            services.Replace(ServiceDescriptor.Singleton<IAuthenticationSchemeProvider, MultiTenantAuthenticationSchemeProvider>());
            services.Replace(ServiceDescriptor.Scoped<IAuthenticationService, MultiTenantAuthenticationService>());

            services.TryAddSingleton<IRemoteAuthenticationMultiTenantStrategy, RemoteAuthenticationMultiTenantStrategy>();

            return this;
        }

        /// <summary>
        /// Adds and configures a <c>IMultiTenantStrategy</c> to the application using its default constructor using dependency injection.
        /// </summary>>
        /// <param name="parameters">a paramter list for any constructor paramaters not covered by dependency injection.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithStore<T>(params object[] parameters) where T : IMultiTenantStore
            => WithStore(sp => ActivatorUtilities.CreateInstance<T>(sp, parameters));

        /// <summary>
        /// Adds and configures a <c>IMultiTenantStrategy</c> to the application using its default constructor using a factory method.
        /// </summary>
        /// <param name="factory">A delegate that will create and configure the strategy.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithStore(Func<IServiceProvider, IMultiTenantStore> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            services.TryAddSingleton<IMultiTenantStore>(sp => factory(sp));

            return this;
        }

        /// <summary>
        /// Adds an empty, case-insensitive InMemoryMultiTenantStore to the application.
        /// </summary>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithInMemoryStore() => WithInMemoryStore(true);

        /// <summary>
        /// Adds an empty InMemoryMultiTenantStore to the application.
        /// </summary>
        /// <param name="ignoreCase">Whether the store should ignore case.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithInMemoryStore(bool ignoreCase) => WithInMemoryStore(_ => { }, ignoreCase);

        /// <summary>
        /// Adds and configures a case-insensitive <c>InMemoryMultiTenantStore</c> to the application using the provided <c>ConfigurationSeciont</c>.
        /// </summary>
        /// <param name="config">The <c>ConfigurationSection</c> which contains the <c>InMemoryMultiTenantStore</c> configuartion settings.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithInMemoryStore(IConfigurationSection configurationSection) =>
            WithInMemoryStore(o => configurationSection.Bind(o), true);

        /// <summary>
        /// Adds and configures <c>InMemoryMultiTenantStore</c> to the application using the provided <c>ConfigurationSeciont</c>.
        /// </summary>
        /// <param name="config">The <c>ConfigurationSection</c> which contains the <c>InMemoryMultiTenantStore</c> configuartion settings.</param>
        /// <param name="ignoreCase">Whether the store should ignore case.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithInMemoryStore(IConfigurationSection configurationSection, bool ignoreCase) =>
            WithInMemoryStore(o => configurationSection.Bind(o), ignoreCase);

        /// <summary>
        /// Adds and configures a case-insensitive <c>InMemoryMultiTenantStore</c> to the application using the provided action.
        /// </summary>
        /// <param name="config">A delegate or lambda for configuring the tenant.</param>
        /// <param name="ignoreCase">Whether the store should ignore case.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithInMemoryStore(Action<InMemoryMultiTenantStoreOptions> config)
            => WithInMemoryStore(config, true);

        /// <summary>
        /// Adds and configures <c>InMemoryMultiTenantStore</c> to the application using the provided action.
        /// </summary>
        /// <param name="config">A delegate or lambda for configuring the tenant.</param>
        /// <param name="ignoreCase">Whether the store should ignore case.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithInMemoryStore(Action<InMemoryMultiTenantStoreOptions> config, bool ignoreCase)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return WithStore(sp => InMemoryStoreFactory(config, ignoreCase, sp.GetService<ILogger<InMemoryMultiTenantStore>>()));
        }

        /// <summary>
        /// Creates an <c>InMemoryMultiTenantStore</c> from configured <c>InMemoryMultiTenantStoreOptions</c>.
        /// </summary>
        private InMemoryMultiTenantStore InMemoryStoreFactory(Action<InMemoryMultiTenantStoreOptions> config, bool ignoreCase, ILogger<InMemoryMultiTenantStore> logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var options = new InMemoryMultiTenantStoreOptions();
            config(options);
            var store = new InMemoryMultiTenantStore(ignoreCase, logger);

            try
            {
                foreach (var tenantConfig in options.TenantConfigurations ?? new InMemoryMultiTenantStoreOptions.TenantConfiguration[0])
                {
                    if (string.IsNullOrWhiteSpace(tenantConfig.Id) ||
                        string.IsNullOrWhiteSpace(tenantConfig.Identifier))
                        throw new MultiTenantException("Tenant Id and Identifer cannot be null or whitespace.");

                    var tenantInfo = new TenantInfo(tenantConfig.Id,
                                               tenantConfig.Identifier,
                                               tenantConfig.Name,
                                               tenantConfig.ConnectionString ?? options.DefaultConnectionString,
                                               null);

                    foreach (var item in tenantConfig.Items ?? new Dictionary<string, string>())
                    {
                        tenantInfo.Items.Add(item.Key, item.Value);
                    }

                    if (!store.TryAddAsync(tenantInfo).Result)
                        throw new MultiTenantException($"Unable to add {tenantInfo.Identifier} is already configured.");
                }
            }
            catch (Exception e)
            {
                throw new MultiTenantException
                    ("Unable to add tenant to store.", e);
            }

            return store;
        }

        /// <summary>
        /// Adds and configures a <c>StaticMultiTenantStrategy</c> to the application.
        /// </summary>
        /// <param name="identifier">The tenant identifier to use for all tenant resolution.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithStaticStrategy(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException("Invalid value for \"identifier\"", nameof(identifier));
            }

            return WithStrategy(sp => new StaticMultiTenantStrategy(identifier, sp.GetService<ILogger<StaticMultiTenantStrategy>>()));
        }

        /// <summary>
        /// Adds and configures a <c>BasePathMultiTenantStrategy</c> to the application.
        /// </summary>
        /// <returnsThe same <c>MultiTenantBuilder</c> passed into the method.></returns>
        public MultiTenantBuilder WithBasePathStrategy()
            => WithStrategy(sp => new BasePathMultiTenantStrategy(sp.GetService<ILogger<BasePathMultiTenantStrategy>>()));

        /// <summary>
        /// Adds and configures a <c>RouteMultiTenantStrategy</c> with a route parameter "__tenant__" to the application.
        /// </summary>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithRouteStrategy(Action<IRouteBuilder> configRoutes)
            => WithRouteStrategy("__tenant__", configRoutes);

        /// <summary>
        /// Adds and configures a <c>RouteMultiTenantStrategy</c> to the application.
        /// </summary>
        /// <param name="tenantParam">The name of the route parameter used to determine the tenant identifier.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithRouteStrategy(string tenantParam, Action<IRouteBuilder> configRoutes)
        {
            if (string.IsNullOrWhiteSpace(tenantParam))
            {
                throw new ArgumentException("Invalud value for \"tenantParam\"", nameof(tenantParam));
            }

            if (configRoutes == null)
            {
                throw new ArgumentNullException(nameof(configRoutes));
            }

            return WithStrategy(sp => new RouteMultiTenantStrategy(tenantParam, configRoutes, sp.GetService<ILogger<RouteMultiTenantStrategy>>()));
        }

        /// <summary>
        /// Adds and configures a <c>HostMultiTenantStrategy</c> with template "__tenant__.*" to the application.
        /// </summary>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithHostStrategy()
            => WithHostStrategy("__tenant__.*");

        /// <summary>
        /// Adds and configures a <c>HostMultiTenantStrategy</c> to the application.
        /// </summary>
        /// <param name="template">The template for determining the tenant identifier in the host.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithHostStrategy(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("Invalid value for \"template\"", nameof(template));
            }

            return WithStrategy(sp => new HostMultiTenantStrategy(template, sp.GetService<ILogger<HostMultiTenantStrategy>>()));
        }

        /// <summary>
        /// Adds and configures a <c>IMultiTenantStrategy</c> to the application using its default constructor using dependency injection.
        /// </summary>
        /// <param name="parameters">a paramter list for any constructor paramaters not covered by dependency injection.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithStrategy<T>(params object[] parameters) where T : IMultiTenantStrategy
            => WithStrategy(sp => ActivatorUtilities.CreateInstance<T>(sp, parameters));

        /// <summary>
        /// Adds and configures a <c>IMultiTenantStrategy</c> to the application using a factory method.
        /// </summary>
        /// <param name="factory">A delegate that will create and configure the strategy.</param>
        /// <returns>The same <c>MultiTenantBuilder</c> passed into the method.</returns>
        public MultiTenantBuilder WithStrategy(Func<IServiceProvider, IMultiTenantStrategy> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            services.TryAddSingleton<IMultiTenantStrategy>(factory);

            return this;
        }
    }
}
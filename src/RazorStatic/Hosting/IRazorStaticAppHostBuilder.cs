using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace RazorStatic.Hosting;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IRazorStaticAppHostBuilder
{
    IDictionary<object, object> Properties { get; }

    IRazorStaticAppHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configure);

    IRazorStaticAppHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configure);

    IRazorStaticAppHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure);

    IRazorStaticAppHostBuilder UseServiceProviderFactory<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull;

    IRazorStaticAppHostBuilder UseServiceProviderFactory<TContainerBuilder>(
        Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        where TContainerBuilder : notnull;

    IRazorStaticAppHostBuilder ConfigureContainer<TContainerBuilder>(
        Action<HostBuilderContext, TContainerBuilder> configure);

    IRazorStaticAppHost Build();
}
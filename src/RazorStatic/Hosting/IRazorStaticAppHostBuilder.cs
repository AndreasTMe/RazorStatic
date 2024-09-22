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
    /// <summary>
    /// 
    /// </summary>
    IDictionary<object, object> Properties { get; }

    /// <summary>
    /// /
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    IRazorStaticAppHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configure);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    IRazorStaticAppHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configure);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configure"></param>
    /// <returns></returns>
    IRazorStaticAppHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory"></param>
    /// <typeparam name="TContainerBuilder"></typeparam>
    /// <returns></returns>
    IRazorStaticAppHostBuilder UseServiceProviderFactory<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory)
        where TContainerBuilder : notnull;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory"></param>
    /// <typeparam name="TContainerBuilder"></typeparam>
    /// <returns></returns>
    IRazorStaticAppHostBuilder UseServiceProviderFactory<TContainerBuilder>(
        Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        where TContainerBuilder : notnull;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configure"></param>
    /// <typeparam name="TContainerBuilder"></typeparam>
    /// <returns></returns>
    IRazorStaticAppHostBuilder ConfigureContainer<TContainerBuilder>(
        Action<HostBuilderContext, TContainerBuilder> configure);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IRazorStaticAppHost Build();
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace RazorStatic.Hosting;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IRazorStaticAppHostBuilder
{
    IRazorStaticAppHostBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configure);

    IRazorStaticAppHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure);

    IRazorStaticAppHost Build();
}
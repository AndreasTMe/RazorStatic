using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace RazorStatic.Hosting;

internal sealed class RazorStaticAppHostBuilder : IRazorStaticAppHostBuilder
{
    private readonly IHostBuilder _builder;

    public IDictionary<object, object> Properties => _builder.Properties;

    public RazorStaticAppHostBuilder(IHostBuilder builder) => _builder = builder;

    public IRazorStaticAppHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configure)
    {
        _builder.ConfigureHostConfiguration(configure);
        return this;
    }

    public IRazorStaticAppHostBuilder ConfigureAppConfiguration(
        Action<HostBuilderContext, IConfigurationBuilder> configure)
    {
        _builder.ConfigureAppConfiguration(configure);
        return this;
    }

    public IRazorStaticAppHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configure)
    {
        _builder.ConfigureServices(configure);
        return this;
    }

    public IRazorStaticAppHost Build() => new RazorStaticAppHost(_builder.Build());
}
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorStatic.Configuration;
using RazorStatic.Core;
using RazorStatic.FileSystem;
using RazorStatic.Shared;
using System;
using System.Linq;

namespace RazorStatic.Hosting;

/// <summary>
/// TODO: Documentation
/// </summary>
public static class RazorStaticApp
{
    public static IRazorStaticAppHostBuilder CreateBuilder() => CreateBuilder(null, null);

    public static IRazorStaticAppHostBuilder CreateBuilder(string[]? args) => CreateBuilder(args, null);

    public static IRazorStaticAppHostBuilder CreateBuilder(Action<RazorStaticConfigurationOptions>? configure) =>
        CreateBuilder(null, configure);

    public static IRazorStaticAppHostBuilder CreateBuilder(string[]? args,
                                                           Action<RazorStaticConfigurationOptions>? configure)
    {
        var builder = Host.CreateDefaultBuilder(args);

        builder.ConfigureServices(
            (_, services) =>
            {
                services.AddLogging();
                services.AddSingleton<HtmlRenderer>(
                    provider => new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>()));

                services.AddOptions<RazorStaticConfigurationOptions>()
                        .Configure(
                            options =>
                            {
                                configure?.Invoke(options);

                                options.AddCommandLineArgs(args);
                                options.Evaluate();
                            });

                var assembliesTypes = AppDomain.CurrentDomain.GetAssemblies()
                                               .SelectMany(assembly => assembly.GetTypes())
                                               .Where(type => !type.IsInterface)
                                               .ToList();

                if (assembliesTypes.FirstOrDefault(
                        type => typeof(IPagesStore)
                            .IsAssignableFrom(type)) is { } pagesStore)
                {
                    services.AddSingleton(typeof(IPagesStore), pagesStore);
                }

                if (assembliesTypes.FirstOrDefault(
                        type => typeof(IPageCollectionsStore)
                            .IsAssignableFrom(type)) is { } pagesStoreFactory)
                {
                    services.AddSingleton(typeof(IPageCollectionsStore), pagesStoreFactory);
                }

                if (assembliesTypes.FirstOrDefault(
                        type => typeof(ITailwindBuilder)
                            .IsAssignableFrom(type)) is { } tailwindConfig)
                {
                    services.AddSingleton(typeof(ITailwindBuilder), tailwindConfig);
                }

                services.AddTransient<IFileWriter, FileWriter>();
                services.AddTransient<IRazorStaticRenderer, RazorStaticRenderer>();

                var options = services.BuildServiceProvider()
                                      .GetRequiredService<IOptions<RazorStaticConfigurationOptions>>()
                                      .Value;

                if (options.ShouldServe)
                    services.AddHostedService<RazorStaticHostedService>();
            });

        return new RazorStaticAppHostBuilder(builder);
    }
}
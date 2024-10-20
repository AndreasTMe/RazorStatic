using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorStatic.Configuration;
using RazorStatic.Core;
using RazorStatic.FileSystem;
using RazorStatic.Shared;
using RazorStatic.Utilities;
using System;
using System.Collections.Generic;
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

                services.AddSingletonOrNull<IDirectoriesSetup, NullDirectoriesSetup>(assembliesTypes);
                services.AddSingletonOrNull<IPagesStore, NullPagesStore>(assembliesTypes);
                services.AddSingletonOrNull<IPageCollectionsStore, NullPageCollectionsStore>(assembliesTypes);
                services.AddSingletonOrNull<ITailwindBuilder, NullTailwindBuilder>(assembliesTypes);

                services.AddTransient<IFileWriter, FileWriter>();
                services.AddTransient<IStaticContentHandler, StaticContentHandler>();
                services.AddTransient<IRazorStaticRenderer, RazorStaticRenderer>();

                var options = services.BuildServiceProvider()
                                      .GetRequiredService<IOptions<RazorStaticConfigurationOptions>>()
                                      .Value;

                if (options.ShouldServe)
                    services.AddHostedService<RazorStaticHostedService>();
            });
        return new RazorStaticAppHostBuilder(builder);
    }

    private static void AddSingletonOrNull<TService, TNullImplementation>(this IServiceCollection services,
                                                                          List<Type> types)
        where TService : class
        where TNullImplementation : TService, new()
    {
        if (types.FirstOrDefault(type => typeof(TService).IsAssignableFrom(type)) is { } implementationType)
        {
            services.AddSingleton(typeof(TService), implementationType);
        }
        else
        {
            services.AddSingleton<TService>(new TNullImplementation());
        }
    }
}
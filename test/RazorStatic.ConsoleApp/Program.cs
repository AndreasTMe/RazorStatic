using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RazorStatic.ConsoleApp.Services;
using RazorStatic.Hosting;

var host = RazorStaticApp.CreateBuilder(args)
    .ConfigureServices(
        (_, services) =>
        {
            services.AddLogging(
                builder =>
                {
                    builder.AddSimpleConsole(
                        options =>
                        {
                            options.IncludeScopes   = true;
                            options.SingleLine      = true;
                            options.TimestampFormat = "HH:mm:ss ";
                        });
                });

            services.AddSingleton<ITestService, TestService>();
        })
    .Build();

await host.RunAsync();
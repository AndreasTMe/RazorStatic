using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorStatic.Configuration;
using RazorStatic.Core;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Hosting;

internal sealed class RazorStaticAppHost : IRazorStaticAppHost
{
    private readonly IHost _host;

    public RazorStaticAppHost(IHost host) => _host = host;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var cts = cancellationToken == CancellationToken.None
            ? new CancellationTokenSource()
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await using (var scope = _host.Services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger<RazorStaticAppHost>();

                var sw = new Stopwatch();
                sw.Start();

                await scope.ServiceProvider.GetRequiredService<IStaticContentHandler>()
                    .HandleAsync(cts.Token)
                    .ConfigureAwait(false);
                await scope.ServiceProvider.GetRequiredService<IRazorStaticRenderer>()
                    .RenderAsync(cts.Token)
                    .ConfigureAwait(false);

                sw.Stop();

                logger.LogInformation("Rendering elapsed time: {Milliseconds}ms.", sw.ElapsedMilliseconds);

                if (scope.ServiceProvider.GetRequiredService<IOptions<RazorStaticConfigurationOptions>>()
                        .Value is not { ShouldServe: true })
                {
                    return;
                }
            }

            await _host.StartAsync(cts.Token).ConfigureAwait(false);
            await _host.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
        }
        finally
        {
            if (_host is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _host.Dispose();
            }
        }
    }
}
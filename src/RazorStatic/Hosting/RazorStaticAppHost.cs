using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RazorStatic.Configuration;
using RazorStatic.Core;
using RazorStatic.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Hosting;

internal sealed class RazorStaticAppHost : IRazorStaticAppHost
{
    private readonly IHost                           _host;
    private readonly IRazorStaticRenderer            _renderer;
    private readonly ITailwindBuilder                _tailwindBuilder;
    private readonly RazorStaticConfigurationOptions _configuration;

    public RazorStaticAppHost(IHost host)
    {
        _host            = host;
        _renderer        = host.Services.GetRequiredService<IRazorStaticRenderer>();
        _tailwindBuilder = host.Services.GetRequiredService<ITailwindBuilder>();
        _configuration   = host.Services.GetRequiredService<IOptions<RazorStaticConfigurationOptions>>().Value;
    }

    public async Task RunAsync()
    {
        var cts = new CancellationTokenSource();

        try
        {
            await _host.StartAsync(cts.Token).ConfigureAwait(false);

            await _tailwindBuilder.BuildAsync().ConfigureAwait(false);
            await _renderer.RenderAsync().ConfigureAwait(false);

            if (_configuration.ShouldServe)
                await _host.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
        }
        finally
        {
            if (_host is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                _host.Dispose();
        }
    }
}
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorStatic.Configuration;
using RazorStatic.Utilities;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Hosting;

internal sealed class RazorStaticHostedService : IHostedService
{
    private readonly string                            _404FilePath;
    private readonly string                            _500FilePath;
    private readonly CancellationTokenSource           _cts;
    private readonly ILogger<RazorStaticHostedService> _logger;
    private readonly RazorStaticConfigurationOptions   _options;
    private readonly HttpListener                      _server;

    private readonly SemaphoreSlim _semaphore;
    private readonly string        _sourcePath;

    public RazorStaticHostedService(ILogger<RazorStaticHostedService> logger,
                                    IOptions<RazorStaticConfigurationOptions> options)
    {
        _logger             = logger;
        _options            = options.Value;
        _options.OutputPath = _options.OutputPath.Trim(Path.DirectorySeparatorChar);

        _sourcePath = _options.IsAbsoluteOutputPath
            ? _options.OutputPath
            : @$"{Environment.CurrentDirectory}\{_options.OutputPath}";
        _404FilePath = _sourcePath + Path.DirectorySeparatorChar + Constants.Page.Error404 + ".html";
        _500FilePath = _sourcePath + Path.DirectorySeparatorChar + Constants.Page.Error500 + ".html";

        _semaphore = new SemaphoreSlim(1, 1);

        _server = new HttpListener();
        _cts    = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var uri = $"http://localhost:{_options.Port}/";
        _server.Prefixes.Add(uri);
        _server.Start();

        _logger.LogInformation("Serving files from '{Path}' at: '{Uri}'.", _sourcePath, uri);

        _ = Task.Run(
            async () =>
            {
                var token = _cts.Token;

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            var contextTask = _server.GetContextAsync();
                            var completedTask = await Task.WhenAny(contextTask, Task.Delay(-1, token))
                                                          .ConfigureAwait(false);

                            if (completedTask != contextTask) continue;

                            var context = await contextTask.ConfigureAwait(false);

                            await _semaphore.WaitAsync(token);

                            _ = HandleRequestAsync(context, token)
                                .ContinueWith(_ => _semaphore.Release(), token);
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogDebug("Cancellation requested for HttpListener.");
                            break;
                        }
                        catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                        {
                            _logger.LogDebug("HttpListener operation aborted.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "An error occurred while handling a request.");
                            break;
                        }
                    }
                }
                finally
                {
                    await StopServerIfListeningAsync();
                }
            },
            cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopServerIfListeningAsync();
        _semaphore.Dispose();
    }

    private async Task StopServerIfListeningAsync()
    {
        if (!_server.IsListening)
            return;

        await _semaphore.WaitAsync();

        _server.Stop();
        _server.Close();

        _semaphore.Release();
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var requestUrl = context.Request.Url!.AbsolutePath.Trim('/');
            var filePath   = Path.Combine(_sourcePath, requestUrl);

            if (string.IsNullOrEmpty(requestUrl) || Directory.Exists(filePath))
            {
                filePath = Path.Combine(filePath, "index.html");
            }
            else if (!Path.HasExtension(filePath))
            {
                var htmlFilePath = filePath + ".html";

                if (File.Exists(htmlFilePath)) filePath = htmlFilePath;
            }

            if (File.Exists(filePath))
            {
                var fileContent = await File.ReadAllBytesAsync(filePath, cancellationToken)
                                            .ConfigureAwait(false);
                context.Response.ContentType     = GetContentType(filePath);
                context.Response.ContentLength64 = fileContent.Length;
                await context.Response.OutputStream
                             .WriteAsync(fileContent, cancellationToken)
                             .ConfigureAwait(false);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

                if (File.Exists(_404FilePath))
                {
                    var fileContent = await File.ReadAllBytesAsync(_404FilePath, cancellationToken)
                                                .ConfigureAwait(false);
                    context.Response.ContentType     = GetContentType(_404FilePath);
                    context.Response.ContentLength64 = fileContent.Length;
                    await context.Response.OutputStream
                                 .WriteAsync(fileContent, cancellationToken)
                                 .ConfigureAwait(false);
                }
                else
                {
                    var notFoundMessage = "404 - File Not Found"u8.ToArray();
                    await context.Response.OutputStream
                                 .WriteAsync(notFoundMessage, cancellationToken)
                                 .ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Cancellation requested for page request.");
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            if (File.Exists(_500FilePath))
            {
                var fileContent = await File.ReadAllBytesAsync(_500FilePath, cancellationToken)
                                            .ConfigureAwait(false);
                context.Response.ContentType     = GetContentType(_500FilePath);
                context.Response.ContentLength64 = fileContent.Length;
                await context.Response.OutputStream
                             .WriteAsync(fileContent, cancellationToken)
                             .ConfigureAwait(false);
            }
            else
            {
                var errorMessage = Encoding.UTF8.GetBytes($"500 - Internal Server Error: {ex.Message}");
                await context.Response.OutputStream
                             .WriteAsync(errorMessage, cancellationToken)
                             .ConfigureAwait(false);
            }
        }
        finally
        {
            context.Response.OutputStream.Close();
        }
    }

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLower() switch
        {
            ".html" => "text/html",
            ".css"  => "text/css",
            ".js"   => "application/javascript",
            ".png"  => "image/png",
            ".jpg"  => "image/jpeg",
            ".gif"  => "image/gif",
            ".svg"  => "image/svg+xml",
            _       => "application/octet-stream"
        };
    }
}
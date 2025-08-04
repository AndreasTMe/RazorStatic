using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorStatic.Abstractions;
using RazorStatic.Components;
using RazorStatic.Configuration;
using RazorStatic.FileSystem;
using RazorStatic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal sealed partial class RazorStaticRenderer : IRazorStaticRenderer
{
    private readonly IDirectoriesSetup                         _directoriesSetup;
    private readonly IPagesStore                               _pagesStore;
    private readonly IPageCollectionsStore                     _pageCollectionsStore;
    private readonly IFileWriter                               _fileWriter;
    private readonly IOptions<RazorStaticConfigurationOptions> _options;
    private readonly ILogger<RazorStaticRenderer>              _logger;

    public RazorStaticRenderer(
        IDirectoriesSetup directoriesSetup,
        IPagesStore pagesStore,
        IPageCollectionsStore pageCollectionsStore,
        IFileWriter fileWriter,
        IOptions<RazorStaticConfigurationOptions> options,
        ILogger<RazorStaticRenderer> logger)
    {
        _directoriesSetup     = directoriesSetup;
        _pagesStore           = pagesStore;
        _pageCollectionsStore = pageCollectionsStore;
        _fileWriter           = fileWriter;
        _options              = options;
        _logger               = logger;
    }

    public Task RenderAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_directoriesSetup.Pages))
        {
            _logger.LogError(
                "No project path was defined. Make sure the '{DirectoriesSetup}' was generated using the appropriate attribute.",
                nameof(IDirectoriesSetup));

            return Task.CompletedTask;
        }

        var razorRoutes = Directory.GetFiles(_directoriesSetup.Pages, "*.razor", SearchOption.AllDirectories)
            .Select(static file => new RazorRoute(file))
            .OrderBy(static route => route.Depth)
            .ToList();

        if (razorRoutes.Count == 0)
        {
            _logger.LogInformation("No Razor components found!");
            return Task.CompletedTask;
        }

        var minDepth = razorRoutes[0].Depth;
        foreach (var route in razorRoutes)
        {
            if (route.Depth > minDepth)
            {
                throw new ArgumentException("The root directory should contain an Index razor file");
            }

            if (Path.GetFileNameWithoutExtension(route.FullPath)
                .Equals(Constants.Page.Index, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
        }

        var tasks = new List<Task>();
        foreach (var route in razorRoutes)
        {
            tasks.Add(GeneratePageTaskAsync(route, cancellationToken));
        }

        return TaskUtils.RunBatchAsync(tasks, _options.Value.MaxConcurrentFiles);
    }

    private async Task GeneratePageTaskAsync(RazorRoute route, CancellationToken cancellationToken)
    {
        if (route.IsDynamic)
        {
            var pageType = _pagesStore.GetPageType(route.FullPath);

            if (_pageCollectionsStore.TryGetCollection(route.FullPath, out var collection))
            {
                if (pageType.IsSubclassOf(typeof(CollectionFileComponentBase)))
                {
                    await foreach (var (filePath, pageHtml) in collection.RenderComponentsAsync(
                                       pageType,
                                       cancellationToken))
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
                        if (string.IsNullOrWhiteSpace(pageHtml))
                            return;

                        var fileInfo = FileUtils.GenerateDynamicPageInfo(filePath, collection.RootPath);
                        await _fileWriter.WriteAsync(
                                pageHtml,
                                fileInfo.Name,
                                _options.Value.ActualOutputPath + fileInfo.Directory)
                            .ConfigureAwait(false);

                        _logger.LogInformation(
                            "Rendered '{Page}.html' page successfully.",
                            fileInfo.Directory + fileInfo.Name);
                    }
                }
                else if (pageType.IsSubclassOf(typeof(CollectionFileGroupComponentBase)))
                {
                    await foreach (var (fileName, pageHtml) in collection.RenderGroupComponentsAsync(
                                       pageType,
                                       cancellationToken))
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
                        if (string.IsNullOrWhiteSpace(pageHtml))
                            return;

                        var fileInfo = FileUtils.GenerateGroupedDynamicPageInfo(
                            route.FullPath,
                            _directoriesSetup.Pages,
                            fileName);
                        await _fileWriter.WriteAsync(
                                pageHtml,
                                fileInfo.Name,
                                _options.Value.ActualOutputPath + fileInfo.Directory)
                            .ConfigureAwait(false);

                        _logger.LogInformation(
                            "Rendered '{Page}.html' page successfully.",
                            fileInfo.Directory + fileInfo.Name);
                    }
                }
                else
                {
                    throw new NotSupportedException($"Page type not supported: '{pageType.FullName}'");
                }
            }
        }
        else
        {
            var pageHtml = await _pagesStore.RenderComponentAsync(route.FullPath, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageHtml))
                return;

            var fileInfo = FileUtils.GenerateSimplePageInfo(route.FullPath, _directoriesSetup.Pages);
            await _fileWriter.WriteAsync(pageHtml, fileInfo.Name, _options.Value.ActualOutputPath + fileInfo.Directory)
                .ConfigureAwait(false);

            _logger.LogInformation("Rendered '{Page}.html' page successfully.", fileInfo.Directory + fileInfo.Name);
        }
    }
}
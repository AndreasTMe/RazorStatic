﻿using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RazorStatic.Abstractions;
using RazorStatic.Configuration;
using RazorStatic.FileSystem;
using RazorStatic.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal sealed partial class RazorStaticRenderer : IRazorStaticRenderer
{
    private readonly HtmlRenderer                 _htmlRenderer;
    private readonly IDirectoriesSetup            _directoriesSetup;
    private readonly IPagesStore                  _pagesStore;
    private readonly IPageCollectionsStore        _pageCollectionsStore;
    private readonly IFileWriter                  _fileWriter;
    private readonly ILogger<RazorStaticRenderer> _logger;

    private readonly string _rootPath;

    public RazorStaticRenderer(HtmlRenderer htmlRenderer,
                               IDirectoriesSetup directoriesSetup,
                               IPagesStore pagesStore,
                               IPageCollectionsStore pageCollectionsStore,
                               IFileWriter fileWriter,
                               IOptions<RazorStaticConfigurationOptions> options,
                               ILogger<RazorStaticRenderer> logger)
    {
        _htmlRenderer         = htmlRenderer;
        _directoriesSetup     = directoriesSetup;
        _pagesStore           = pagesStore;
        _pageCollectionsStore = pageCollectionsStore;
        _fileWriter           = fileWriter;
        _logger               = logger;

        _rootPath = options.Value.IsAbsoluteOutputPath
            ? options.Value.OutputPath
            : @$"{Environment.CurrentDirectory}\{options.Value.OutputPath}";
    }

    public async Task RenderAsync()
    {
        if (string.IsNullOrWhiteSpace(_directoriesSetup.Pages))
        {
            _logger.LogError(
                "No project path was defined. Make sure the '{DirectoriesSetup}' was generated using the appropriate attribute.",
                nameof(IDirectoriesSetup));

            return;
        }

        var razorFiles = Directory.GetFiles(_directoriesSetup.Pages, "*.razor", SearchOption.AllDirectories)
                                  .GroupBy(file => file[..file.LastIndexOf(Path.DirectorySeparatorChar)])
                                  .Select(
                                      grouping =>
                                      {
                                          var path = grouping.Key.Replace(_directoriesSetup.Pages, string.Empty);
                                          return new KeyValuePair<NodePath, ImmutableArray<string>>(
                                              new NodePath(path, path.Count(p => p == Path.DirectorySeparatorChar)),
                                              [..grouping]);
                                      })
                                  .OrderBy(kvp => kvp.Key.Path)
                                  .ToImmutableArray();

        if (razorFiles.IsEmpty)
        {
            _logger.LogInformation("No Razor components found!");
            return;
        }

        var topLevelDir = razorFiles[0];
        if (topLevelDir.Value.All(f => Path.GetFileNameWithoutExtension(f) != Constants.Page.Index))
        {
            throw new ArgumentException("The root directory should contain an Index razor file");
        }

        var root = new Node();
        BuildPageTreeRecursive(root, razorFiles, 0);

        var tasks = GeneratePageTasksRecursiveAsync(root);

        var sw = new Stopwatch();
        sw.Start();
        for (var i = 0; i < tasks.Count; i += Constants.BatchSize)
        {
            await Task.WhenAll(tasks.Skip(i).Take(Constants.BatchSize))
                      .ConfigureAwait(false);
        }
        sw.Stop();

        _logger.LogInformation("Rendering elapsed time: {Milliseconds}ms.", sw.ElapsedMilliseconds);
    }

    public ValueTask DisposeAsync() => _htmlRenderer.DisposeAsync();

    private void BuildPageTreeRecursive(Node root,
                                        ImmutableArray<KeyValuePair<NodePath, ImmutableArray<string>>> razorFiles,
                                        int index)
    {
        var directory = razorFiles[index];

        foreach (var file in directory.Value)
            root.AddLeaf(new Leaf(file));

        for (var i = 1; i < razorFiles.Length; i++)
        {
            if (razorFiles[i].Key.Depth != directory.Key.Depth + 1)
                continue;

            if (!razorFiles[i].Key.Path.StartsWith(directory.Key.Path))
                continue;

            var node = new Node();
            BuildPageTreeRecursive(node, razorFiles, i);

            root.AddNode(node);
        }
    }

    private List<Task> GeneratePageTasksRecursiveAsync(Node node)
    {
        var tasks = new List<Task>();

        foreach (var leaf in node.Leaves)
            tasks.Add(GeneratePageTaskAsync(leaf));

        foreach (var childNode in node.Nodes)
            tasks.AddRange(GeneratePageTasksRecursiveAsync(childNode));

        return tasks;
    }

    private async Task GeneratePageTaskAsync(Leaf leaf)
    {
        if (leaf.IsDynamicPath)
        {
            var pageType = _pagesStore.GetPageType(leaf.FullPath);
            if (_pageCollectionsStore.TryGetCollection(leaf.FullPath, out var collection))
            {
                await foreach (var (fileName, pageHtml) in collection.RenderComponentsAsync(leaf.FullPath, pageType))
                {
                    if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(pageHtml))
                        return;

                    var fileInfo = GenerateFileInfo(fileName, collection.RootPath, true);
                    await _fileWriter.WriteAsync(pageHtml, fileInfo.Name, _rootPath + fileInfo.Directory)
                                     .ConfigureAwait(false);

                    _logger.LogInformation(
                        "Rendered '{Page}.html' page successfully.",
                        fileInfo.Directory + fileInfo.Name);
                }
            }
        }
        else
        {
            var pageHtml = await _pagesStore.RenderComponentAsync(leaf.FullPath).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(pageHtml))
                return;

            var fileInfo = GenerateFileInfo(leaf.FullPath, _directoriesSetup.Pages);
            await _fileWriter.WriteAsync(pageHtml, fileInfo.Name, _rootPath + fileInfo.Directory).ConfigureAwait(false);

            _logger.LogInformation("Rendered '{Page}.html' page successfully.", fileInfo.Directory + fileInfo.Name);
        }
    }

    private static FileInfo GenerateFileInfo(string fullPath, string rootPath, bool isCollection = false)
    {
        var directoryName = fullPath[..fullPath.LastIndexOf(Path.DirectorySeparatorChar)]
                            .Replace(rootPath, "")
                            .ToLowerInvariant();
        if (!directoryName.StartsWith(Path.DirectorySeparatorChar))
            directoryName = Path.DirectorySeparatorChar + directoryName;
        if (!directoryName.EndsWith(Path.DirectorySeparatorChar))
            directoryName += Path.DirectorySeparatorChar;

        if (isCollection && directoryName.Length > 1)
        {
            var index = directoryName.IndexOf(Path.DirectorySeparatorChar, 1) + 1;
            if (index < directoryName.Length)
            {
                directoryName = directoryName[..index];
            }
        }

        var fileName = Path.GetFileNameWithoutExtension(fullPath).ToLowerInvariant();
        fileName = SlugUtils.Convert(fileName);

        return Constants.Page.IsReserved(fileName)
            ? new FileInfo(directoryName, fileName)
            : new FileInfo(
                directoryName + fileName + Path.DirectorySeparatorChar,
                Constants.Page.Index.ToLowerInvariant());
    }
}
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using RazorStatic.Abstractions;
using RazorStatic.Components;
using RazorStatic.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.ConsoleApp.Blobs;

internal sealed class CollectionDefinitionFromBlobStorage : AbstractPageCollectionDefinition
{
    private readonly IServiceScopeFactory _scopeFactory;

    public override string RootPath { get; } = string.Empty;

    public CollectionDefinitionFromBlobStorage(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public override async IAsyncEnumerable<RenderedResult> RenderComponentsAsync(
        Type pageType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var scope    = _scopeFactory.CreateAsyncScope();
        await using var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();

        var blobContainerClient = scope.ServiceProvider.GetRequiredService<BlobContainerClient>();

        await foreach (var blobItem in blobContainerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            var slug       = SlugUtils.Convert(Path.GetFileNameWithoutExtension(blobItem.Name).ToLowerInvariant());
            var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);

            (string? Frontmatter, string? Markdown) fileContent;
            await using (var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken))
            {
                using (var reader = new StreamReader(stream))
                {
                    fileContent = await GetFileContentAsync(reader, cancellationToken);
                }
            }

            var content = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        [nameof(CollectionFileComponentBase.ContentFilePath)] = blobItem.Name,
                        [nameof(CollectionFileComponentBase.Slug)]            = slug,
                        [nameof(CollectionFileComponentBase.FrontMatter)]     = fileContent.Frontmatter,
                        [nameof(CollectionFileComponentBase.Content)]         = fileContent.Markdown
                    });

                var output = await renderer.RenderComponentAsync(pageType, parameters);
                return output.ToHtmlString();
            });

            yield return new RenderedResult(blobItem.Name, content);
        }
    }

    public override async IAsyncEnumerable<RenderedResult> RenderGroupComponentsAsync(
        Type pageType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var scope    = _scopeFactory.CreateAsyncScope();
        await using var renderer = scope.ServiceProvider.GetRequiredService<HtmlRenderer>();

        var blobContainerClient = scope.ServiceProvider.GetRequiredService<BlobContainerClient>();

        var metadataGroups = await Extensions.GetMetadataGroupsAsync(
            blobContainerClient,
            GetFileContentAsync,
            cancellationToken);

        if (!metadataGroups.TryGetValue(pageType, out var metadataPerGroup))
        {
            yield break;
        }

        foreach (var (group, metadata) in metadataPerGroup)
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            var slug = SlugUtils.Convert(group.ToLowerInvariant());
            var content = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var parameters = ParameterView.FromDictionary(
                    new Dictionary<string, object?>
                    {
                        [nameof(CollectionFileGroupComponentBase.Slug)]     = slug,
                        [nameof(CollectionFileGroupComponentBase.GroupBy)]  = group,
                        [nameof(CollectionFileGroupComponentBase.Metadata)] = metadata
                    });
                var output = await renderer.RenderComponentAsync(pageType, parameters);
                return output.ToHtmlString();
            });
            yield return new RenderedResult(slug, content);
        }
    }
}

file static class Extensions
{
    private static readonly FrozenDictionary<Type, string> PageTypeToGroupKey = new Dictionary<Type, string>
        {
        }
        .ToFrozenDictionary();

    private static
        FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<ValueTuple<string, string>>>>? _metadataGroups;

    public static async
        Task<FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<(string Slug, string FrontMatter)>>>>
        GetMetadataGroupsAsync(
            BlobContainerClient containerClient,
            Func<TextReader, CancellationToken, Task<(string? FrontMatter, string? Markdown)>> getFileContentAction,
            CancellationToken cancellationToken)
    {
        if (_metadataGroups is not null)
        {
            return _metadataGroups;
        }

        var metadataGroups =
            new ConcurrentDictionary<Type, ConcurrentDictionary<string, ConcurrentBag<ValueTuple<string, string>>>>();

        await Parallel.ForEachAsync(
            containerClient.GetBlobsAsync(cancellationToken: cancellationToken),
            new ParallelOptions
            {
                CancellationToken      = cancellationToken,
                MaxDegreeOfParallelism = 8
            },
            async (blobItem, token) =>
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var slug       = SlugUtils.Convert(Path.GetFileNameWithoutExtension(blobItem.Name).ToLowerInvariant());

                await using var stream = await blobClient.OpenReadAsync(cancellationToken: token);
                using var       reader = new StreamReader(stream);

                var (frontmatter, _) = await getFileContentAction.Invoke(reader, token);
                if (string.IsNullOrWhiteSpace(frontmatter))
                {
                    return;
                }

                foreach (var (type, groupBy) in PageTypeToGroupKey)
                {
                    if (!frontmatter.Split("/r/n").Any(line => line.StartsWith(groupBy, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    if (!metadataGroups.TryGetValue(type, out var groupByToFrontmatters))
                    {
                        groupByToFrontmatters = [];
                        metadataGroups[type]  = groupByToFrontmatters;
                    }

                    if (!groupByToFrontmatters.TryGetValue(groupBy, out var frontmatters))
                    {
                        frontmatters                  = [];
                        metadataGroups[type][groupBy] = frontmatters;
                    }

                    metadataGroups[type][groupBy].Add((slug, frontmatter));
                }
            });

        _metadataGroups = metadataGroups.ToFrozenDictionary(
            static x => x.Key,
            static x => x.Value.ToFrozenDictionary(
                static y => y.Key,
                static y => y.Value.ToFrozenSet()));

        return _metadataGroups;
    }
}
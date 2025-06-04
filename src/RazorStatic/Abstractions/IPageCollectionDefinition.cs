using System;
using System.Collections.Generic;
using System.Threading;

namespace RazorStatic.Abstractions;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPageCollectionDefinition
{
    string RootPath { get; }

    IAsyncEnumerable<RenderedResult> RenderComponentsAsync(Type pageType, CancellationToken cancellationToken);

    IAsyncEnumerable<RenderedResult> RenderGroupComponentsAsync(Type pageType, CancellationToken cancellationToken);
}
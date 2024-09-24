using System;
using System.Collections.Generic;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface ICollectionPagesStore
{
    string RootPath { get; }

    IAsyncEnumerable<RenderedResult> RenderComponentsAsync(Type pageType);
}
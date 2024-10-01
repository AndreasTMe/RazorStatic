using System;
using System.Collections.Generic;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPageCollectionDefinition
{
    string RootPath { get; }

    IAsyncEnumerable<RenderedResult> RenderComponentsAsync(string filePath, Type pageType);
}
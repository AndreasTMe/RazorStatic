using System;
using System.Collections.Generic;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface ICollectionPagesStore
{
    /// <summary>
    /// 
    /// </summary>
    string RootPath { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pageType"></param>
    /// <returns></returns>
    IAsyncEnumerable<RenderedResult> RenderComponentsAsync(Type pageType);
}
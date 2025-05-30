﻿using System;
using System.Collections.Generic;

namespace RazorStatic.Abstractions;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPageCollectionDefinition
{
    string RootPath { get; }

    IAsyncEnumerable<RenderedResult> RenderComponentsAsync(Type pageType);

    IAsyncEnumerable<RenderedResult> RenderGroupComponentsAsync(Type pageType);
}
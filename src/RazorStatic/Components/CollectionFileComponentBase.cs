using Microsoft.AspNetCore.Components;
using RazorStatic.Utilities;
using System;
using System.IO;

namespace RazorStatic.Components;

/// <summary>
/// TODO: Documentation
/// </summary>
public abstract class CollectionFileComponentBase : FileComponentBase
{
    private string _slug = string.Empty;

    [Parameter]
    public string? ContentFilePath { get; set; }

    [Parameter]
    public string? Content { get; set; }

    [Parameter]
    public string? FrontMatter { get; set; }

    protected string Slug
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_slug) && !string.IsNullOrWhiteSpace(ContentFilePath))
            {
                var start = ContentFilePath.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                ArgumentOutOfRangeException.ThrowIfNegative(start);

                var end = ContentFilePath.LastIndexOf('.');
                if (end <= start)
                    end = ContentFilePath.Length;

                _slug = SlugUtils.Convert(ContentFilePath[start..end]);
            }

            return _slug;
        }
    }

    protected static RenderFragment CreateRenderFragment(string html) => b => b.AddMarkupContent(0, html);
}
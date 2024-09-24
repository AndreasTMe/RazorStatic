using Microsoft.AspNetCore.Components;
using System;
using System.IO;

namespace RazorStatic.Shared.Components;

/// <summary>
/// TODO: Documentation
/// </summary>
public abstract class CollectionFileComponentBase : FileComponentBase
{
    private string _contentFileName = string.Empty;

    [Parameter]
    public string? ContentFilePath { get; set; }

    protected string? ContentFileName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_contentFileName) && !string.IsNullOrWhiteSpace(ContentFilePath))
            {
                var start = ContentFilePath.LastIndexOf(Path.DirectorySeparatorChar) + 1;
                ArgumentOutOfRangeException.ThrowIfNegative(start);

                var end = ContentFilePath.LastIndexOf('.');
                if (end <= start)
                    end = ContentFilePath.Length;

                _contentFileName = ContentFilePath[start..end];
            }

            return _contentFileName;
        }
    }

    protected static RenderFragment CreateRenderFragment(string html) => b => b.AddMarkupContent(0, html);
}
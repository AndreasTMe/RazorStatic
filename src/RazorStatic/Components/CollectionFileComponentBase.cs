using Microsoft.AspNetCore.Components;

namespace RazorStatic.Components;

/// <summary>
/// TODO: Documentation
/// </summary>
public abstract class CollectionFileComponentBase : ComponentBase
{
    [Parameter]
    public string? ContentFilePath { get; set; }

    [Parameter]
    public string? Slug { get; set; }

    [Parameter]
    public string? FrontMatter { get; set; }

    [Parameter]
    public string? Content { get; set; }

    protected static RenderFragment CreateRenderFragment(string html) => b => b.AddMarkupContent(0, html);
}
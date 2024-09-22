using Microsoft.AspNetCore.Components;

namespace RazorStatic.Shared.Components;

/// <summary>
/// TODO: Documentation
/// </summary>
public abstract class FileComponentBase : ComponentBase
{
    /// <summary>
    /// 
    /// </summary>
    [Parameter]
    public string? FilePath { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="html"></param>
    /// <returns></returns>
    protected static RenderFragment CreateRenderFragment(string html) => b => b.AddMarkupContent(0, html);
}
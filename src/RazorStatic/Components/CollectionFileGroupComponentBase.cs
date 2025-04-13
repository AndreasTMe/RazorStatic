using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace RazorStatic.Components;

/// <summary>
/// TODO: Documentation
/// </summary>
public abstract class CollectionFileGroupComponentBase : ComponentBase
{
    [Parameter]
    public string? Slug { get; set; }

    [Parameter]
    public string? GroupBy { get; set; }

    [Parameter]
    public IEnumerable<string>? FrontMatters { get; set; }
}
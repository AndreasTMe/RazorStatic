﻿using Microsoft.AspNetCore.Components;

namespace RazorStatic.Shared.Components;

/// <summary>
/// TODO: Documentation
/// </summary>
public abstract class FileComponentBase : ComponentBase
{
    [Parameter]
    public string? Endpoint { get; set; }
}
﻿using Microsoft.AspNetCore.Components;

namespace RazorStatic.Components;

/// <summary>
/// TODO: Documentation
/// </summary>
public abstract class FileComponentBase : ComponentBase
{
    [Parameter]
    public string? PageFilePath { get; set; }
}
using System;

namespace RazorStatic.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class DirectoriesSetupAttribute : Attribute
{
    public string? Pages { get; set; }

    public string? Content { get; set; }

    public string? Static { get; set; }
}
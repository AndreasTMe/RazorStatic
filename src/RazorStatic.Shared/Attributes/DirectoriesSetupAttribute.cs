using System;

namespace RazorStatic.Shared.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class DirectoriesSetupAttribute : Attribute
{
    public required string Pages { get; set; }

    public string? Content { get; set; }

    public string? Tailwind { get; set; }

    public string? Static { get; set; }
}
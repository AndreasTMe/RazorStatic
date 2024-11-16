using System;

namespace RazorStatic.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class StaticContentAttribute : Attribute
{
    public string[]? IncludePaths { get; set; }

    public string[]? Extensions { get; set; }

    public string? EntryFile { get; set; }
}
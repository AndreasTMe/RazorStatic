using System;

namespace RazorStatic.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class CollectionDefinitionAttribute : Attribute
{
    public string? PageRoute { get; set; }

    public string? ContentDirectory { get; set; }
}
using System;

namespace RazorStatic.Shared.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CollectionDefinitionAttribute : Attribute
{
    public required string PageRoute { get; set; }

    public required string ContentDirectory { get; set; }
}
using System;

namespace RazorStatic.Shared.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class CollectionDefinitionAttribute : Attribute
{
    public required string PageRoute { get; set; }

    public required string ContentDirectory { get; set; }
}
using System;

namespace RazorStatic.Shared.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CollectionDefinitionAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public required string PageRoute { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public required string ContentDirectory { get; set; }
}
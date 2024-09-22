using System;

namespace RazorStatic.Shared.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TailwindConfigAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public required string StylesFilePath { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public required string OutputFilePath { get; set; }
}
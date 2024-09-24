using System;

namespace RazorStatic.Shared.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TailwindConfigAttribute : Attribute
{
    public required string StylesFilePath { get; set; }
    
    public required string OutputFilePath { get; set; }
}
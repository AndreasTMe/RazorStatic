using System;

namespace RazorStatic.Shared.Attributes;

/// <summary>
/// TODO: Documentation
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class TailwindConfigAttribute : Attribute
{
    public required string RootFilePath { get; set; }
}
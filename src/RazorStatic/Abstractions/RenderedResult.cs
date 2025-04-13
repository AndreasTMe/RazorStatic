namespace RazorStatic.Abstractions;

/// <summary>
/// TODO: Documentation
/// </summary>
/// <param name="FileNameOrPath"></param>
/// <param name="Content"></param>
public readonly record struct RenderedResult(string FileNameOrPath, string Content);
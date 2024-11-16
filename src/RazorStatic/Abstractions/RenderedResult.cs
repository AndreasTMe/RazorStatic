namespace RazorStatic.Abstractions;

/// <summary>
/// TODO: Documentation
/// </summary>
/// <param name="FileName"></param>
/// <param name="Content"></param>
public readonly record struct RenderedResult(string FileName, string Content);
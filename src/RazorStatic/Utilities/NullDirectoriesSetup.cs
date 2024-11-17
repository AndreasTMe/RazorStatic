using RazorStatic.Abstractions;

namespace RazorStatic.Utilities;

internal sealed class NullDirectoriesSetup : IDirectoriesSetup
{
    public string ProjectRoot { get; } = string.Empty;
    public string Pages       { get; } = string.Empty;
    public string Content     { get; } = string.Empty;
}
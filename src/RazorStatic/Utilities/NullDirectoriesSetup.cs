using RazorStatic.Abstractions;

namespace RazorStatic.Utilities;

internal sealed class NullDirectoriesSetup : IDirectoriesSetup
{
    public string Pages    { get; } = string.Empty;
    public string Content  { get; } = string.Empty;
    public string Tailwind { get; } = string.Empty;
    public string Static   { get; } = string.Empty;
}
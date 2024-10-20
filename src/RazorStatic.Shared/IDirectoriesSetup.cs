namespace RazorStatic.Shared;

public interface IDirectoriesSetup
{
    string Pages    { get; }
    string Content  { get; }
    string Tailwind { get; }
    string Static   { get; }
}
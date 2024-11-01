namespace RazorStatic.Shared;

public interface IDirectoriesSetup
{
    string Pages    { get; }
    string Content  { get; }
    string Static   { get; }
}
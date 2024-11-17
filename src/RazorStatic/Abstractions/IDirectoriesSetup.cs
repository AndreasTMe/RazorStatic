namespace RazorStatic.Abstractions;

public interface IDirectoriesSetup
{
    string ProjectRoot { get; }
    string Pages       { get; }
    string Content     { get; }
}
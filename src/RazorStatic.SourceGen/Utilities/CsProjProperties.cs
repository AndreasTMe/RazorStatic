namespace RazorStatic.SourceGen.Utilities;

internal sealed record CsProjProperties
{
    public string ProjectDir { get; }
    public string OutputPath { get; }

    public CsProjProperties(string projectDir, string outputPath)
    {
        ProjectDir = projectDir;
        OutputPath = outputPath;
    }
}
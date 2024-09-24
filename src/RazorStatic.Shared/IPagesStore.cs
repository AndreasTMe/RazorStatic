using System;
using System.Threading.Tasks;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPagesStore
{
    string RootPath { get; }

    Type GetPageType(string filePath);

    Task<string> RenderComponentAsync(string filePath);

    Task<string> RenderLayoutComponentAsync(string filePath, string htmlBody);
}
using System;
using System.Threading.Tasks;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPagesStore
{
    Type GetPageType(string filePath);

    Task<string> RenderComponentAsync(string filePath);
}
using System;
using System.Threading.Tasks;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPagesStore
{
    /// <summary>
    /// 
    /// </summary>
    string RootPath { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    Type GetPageType(string filePath);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    Task<string> RenderComponentAsync(string filePath);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="htmlBody"></param>
    /// <returns></returns>
    Task<string> RenderLayoutComponentAsync(string filePath, string htmlBody);
}
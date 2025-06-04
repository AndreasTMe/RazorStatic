using System;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Abstractions;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPagesStore
{
    Type GetPageType(string filePath);

    Task<string> RenderComponentAsync(string filePath, CancellationToken cancellationToken);
}
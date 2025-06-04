using RazorStatic.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Utilities;

internal sealed class NullPagesStore : IPagesStore
{
    public Type GetPageType(string filePath) => null!;

    public Task<string> RenderComponentAsync(string filePath, CancellationToken cancellationToken) =>
        Task.FromResult(string.Empty);
}
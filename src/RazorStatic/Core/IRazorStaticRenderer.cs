using System;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal interface IRazorStaticRenderer : IAsyncDisposable
{
    Task RenderAsync(CancellationToken cancellationToken);
}
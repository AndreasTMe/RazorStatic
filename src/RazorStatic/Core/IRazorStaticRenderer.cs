using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal interface IRazorStaticRenderer
{
    Task RenderAsync(CancellationToken cancellationToken);
}
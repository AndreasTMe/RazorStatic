using RazorStatic.Shared;
using System.Threading.Tasks;

namespace RazorStatic.Utilities;

internal sealed class NullTailwindBuilder : ITailwindBuilder
{
    public Task BuildAsync() => Task.CompletedTask;
}
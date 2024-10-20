using System.Threading.Tasks;

namespace RazorStatic.Core;

internal interface IStaticContentHandler
{
    Task HandleAsync();
}
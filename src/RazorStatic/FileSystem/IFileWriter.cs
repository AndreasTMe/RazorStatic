using System.Threading.Tasks;

namespace RazorStatic.FileSystem;

internal interface IFileWriter
{
    Task WriteAsync(string content, string fileName, string fullPath);
}
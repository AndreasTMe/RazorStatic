using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace RazorStatic.FileSystem;

internal sealed class FileWriter : IFileWriter
{
    public Task WriteAsync(string content, string fileName, string fullPath)
    {
        Directory.CreateDirectory(fullPath);
        return File.WriteAllTextAsync(fullPath + $"{fileName}.html", content, Encoding.UTF8);
    }
}
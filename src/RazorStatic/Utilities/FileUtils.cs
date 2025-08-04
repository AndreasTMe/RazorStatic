using System.IO;

namespace RazorStatic.Utilities;

internal readonly record struct RouteFileInfo(string Directory, string Name);

internal static class FileUtils
{
    public static RouteFileInfo GenerateSimplePageInfo(string fullPath, string rootPath)
    {
        var directoryName = GenerateDirectoryName(fullPath, rootPath);

        var fileName = Path.GetFileNameWithoutExtension(fullPath).ToLowerInvariant();
        fileName = SlugUtils.Convert(fileName);

        return GenerateFileInfo(fileName, directoryName);
    }

    public static RouteFileInfo GenerateDynamicPageInfo(string fullPath, string rootPath)
    {
        var directoryName = GenerateDirectoryName(fullPath, rootPath);

        if (directoryName.Length > 1)
        {
            // Needed check in case the content files are in subdirectories
            var index = directoryName.IndexOf(Path.DirectorySeparatorChar, 1) + 1;
            if (index < directoryName.Length)
            {
                directoryName = directoryName[..index];
            }
        }

        var fileName = Path.GetFileNameWithoutExtension(fullPath).ToLowerInvariant();
        fileName = SlugUtils.Convert(fileName);

        return GenerateFileInfo(fileName, directoryName);
    }

    public static RouteFileInfo GenerateGroupedDynamicPageInfo(string fullPath, string rootPath, string groupPath)
    {
        var directoryName = GenerateDirectoryName(fullPath, rootPath);
        var fileName      = SlugUtils.Convert(groupPath.ToLowerInvariant());

        return GenerateFileInfo(fileName, directoryName);
    }

    private static string GenerateDirectoryName(string fullPath, string rootPath)
    {
        var directoryName = fullPath[..fullPath.LastIndexOf(Path.DirectorySeparatorChar)]
            .Replace(rootPath, "")
            .ToLowerInvariant();
        if (!directoryName.StartsWith(Path.DirectorySeparatorChar))
        {
            directoryName = Path.DirectorySeparatorChar + directoryName;
        }
        if (!directoryName.EndsWith(Path.DirectorySeparatorChar))
        {
            directoryName += Path.DirectorySeparatorChar;
        }
        return directoryName;
    }

    private static RouteFileInfo GenerateFileInfo(string fileName, string directoryName) =>
        Constants.Page.IsReserved(fileName)
            ? new RouteFileInfo(directoryName, fileName)
            : new RouteFileInfo(
                directoryName + fileName + Path.DirectorySeparatorChar,
                Constants.Page.Index.ToLowerInvariant());
}
using RazorStatic.Shared;
using RazorStatic.Shared.Attributes;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal sealed class StaticContentHandler : IStaticContentHandler
{
    private readonly IDirectoriesSetup _directoriesSetup;

    public StaticContentHandler(IDirectoriesSetup directoriesSetup) => _directoriesSetup = directoriesSetup;

    public Task HandleAsync()
    {
        if (!string.IsNullOrWhiteSpace(_directoriesSetup.Static))
        {
            var directoriesSetupAttributes = Assembly.GetEntryAssembly()
                                                     ?.GetCustomAttributes()
                                                     .Where(a => a is DirectoriesSetupAttribute)
                                                     .Cast<DirectoriesSetupAttribute>()
                                                     .ToArray()
                                             ?? [];
            Debug.Assert(directoriesSetupAttributes.Length == 1);
            Debug.Assert(!string.IsNullOrWhiteSpace(directoriesSetupAttributes[0].Static));

            var targetDir = Path.Combine(Environment.CurrentDirectory, directoriesSetupAttributes[0].Static!);
            CopyDirectories(_directoriesSetup.Static, targetDir);
        }

        return Task.CompletedTask;
    }

    private static void CopyDirectories(string sourceDirectory, string targetDirectory)
    {
        var diSource = new DirectoryInfo(sourceDirectory);
        var diTarget = new DirectoryInfo(targetDirectory);

        CopyDirectoriesRecursive(diSource, diTarget);
    }

    private static void CopyDirectoriesRecursive(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        foreach (var fi in source.GetFiles())
        {
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        foreach (var diSourceSubDir in source.GetDirectories())
        {
            var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyDirectoriesRecursive(diSourceSubDir, nextTargetSubDir);
        }
    }
}
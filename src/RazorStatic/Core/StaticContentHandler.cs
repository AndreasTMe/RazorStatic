using Microsoft.Extensions.Options;
using RazorStatic.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal sealed class StaticContentHandler : IStaticContentHandler
{
    private readonly IOptions<RazorStaticConfigurationOptions> _options;

    public StaticContentHandler(IOptions<RazorStaticConfigurationOptions> options) => _options = options;

    // if (!string.IsNullOrWhiteSpace(_directoriesSetup.Static))
    // {
    //     var source = new DirectoryInfo(_directoriesSetup.Static);
    //     
    //     var finalCopyTasks = new List<Task>();
    //     
    //     var cssCopyTasks = CopyToOutput(source, "*.css", Constants.Static.CssDirectory);
    //     if (cssCopyTasks.Count > 0)
    //     {
    //         finalCopyTasks.AddRange(cssCopyTasks);
    //     }
    //     
    //     var jsCopyTasks = CopyToOutput(source, "*.js", Constants.Static.JsDirectory);
    //     if (jsCopyTasks.Count > 0)
    //     {
    //         finalCopyTasks.AddRange(finalCopyTasks);
    //     }
    //     
    //     if (finalCopyTasks.Count > 0)
    //     {
    //         await Task.WhenAll(finalCopyTasks).ConfigureAwait(false);
    //     }
    // }
    public Task HandleAsync() => Task.CompletedTask;

    private List<Task> CopyToOutput(DirectoryInfo source, string fileExtension, string targetDirName)
    {
        var files = source.GetFiles(fileExtension, SearchOption.AllDirectories);
        if (files.Length <= 0)
        {
            return [];
        }

        var dir = Path.Combine(Environment.CurrentDirectory, _options.Value.OutputPath, targetDirName);
        Directory.CreateDirectory(dir);

        var commonDirectory       = GetCommonDirectory(source.FullName, files);
        var isNullOrWhiteSpaceDir = string.IsNullOrWhiteSpace(commonDirectory);

        return files.Select(
                        file => Task.Run(
                            () =>
                            {
                                var actualDir = dir;
                                if (!isNullOrWhiteSpaceDir)
                                {
                                    var subDir = file.DirectoryName
                                                     ?.Replace(commonDirectory, string.Empty)
                                                     .TrimStart(Path.DirectorySeparatorChar)
                                                 ?? string.Empty;

                                    if (!string.IsNullOrWhiteSpace(subDir))
                                    {
                                        actualDir = Path.Combine(dir, subDir);
                                        Directory.CreateDirectory(actualDir);
                                    }
                                }

                                file.CopyTo(Path.Combine(actualDir, file.Name), true);
                            }))
                    .ToList();
    }

    private static string GetCommonDirectory(string sourceDirectory, FileInfo[] files)
    {
        var directories = files.Select(f => f.DirectoryName?.Replace(sourceDirectory, string.Empty))
                               .Where(n => n is not null)
                               .Select(n => n!.Split(Path.DirectorySeparatorChar))
                               .ToArray();

        if (directories.Length == 0 || directories[0].Length == 0)
        {
            return sourceDirectory;
        }

        var index = 0;
        while (true)
        {
            if (index >= directories[0].Length)
            {
                return sourceDirectory;
            }

            var current = directories[0][index];

            for (var i = 1; i < directories.Length; i++)
            {
                if (index >= directories[i].Length || current != directories[i][index])
                {
                    return sourceDirectory;
                }
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                sourceDirectory += Path.DirectorySeparatorChar + current;
            }

            index++;
        }
    }
}
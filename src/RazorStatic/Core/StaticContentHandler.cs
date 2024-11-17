using Microsoft.Extensions.Options;
using RazorStatic.Abstractions;
using RazorStatic.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal sealed class StaticContentHandler : IStaticContentHandler
{
    private readonly IDirectoriesSetup                         _directories;
    private readonly IDirectoriesSetupForStaticContent         _directoriesStaticContent;
    private readonly IOptions<RazorStaticConfigurationOptions> _options;

    public StaticContentHandler(IDirectoriesSetup directories,
                                IDirectoriesSetupForStaticContent directoriesStaticContent,
                                IOptions<RazorStaticConfigurationOptions> options)
    {
        _directories              = directories;
        _directoriesStaticContent = directoriesStaticContent;
        _options                  = options;
    }

    public Task HandleAsync()
    {
        var handleTasks = new List<Task>();
        var projectRoot = _directories.ProjectRoot;

        foreach (var (rootPath, extensions, entryFile) in _directoriesStaticContent)
        {
            if (extensions is not { Length: > 0 } && string.IsNullOrWhiteSpace(entryFile))
                continue;

            var currentRoot      = new DirectoryInfo(Path.Combine(projectRoot, rootPath));
            var indexOfSeparator = rootPath.IndexOf(Path.DirectorySeparatorChar) + 1;
            var targetDirName    = indexOfSeparator != 0 ? rootPath[indexOfSeparator..] : rootPath;
            targetDirName = targetDirName.StartsWith('_') ? targetDirName : "_" + targetDirName;

            if (IsFileOfType(".css", extensions, entryFile))
            {
                handleTasks.AddRange(HandleCssFiles(currentRoot, entryFile, targetDirName));
            }
            else if (IsFileOfType(".js", extensions, entryFile))
            {
                handleTasks.AddRange(HandleJsFiles(currentRoot, entryFile, targetDirName));
            }

            // TODO: Handle more extensions, other than .css and .js
        }

        return handleTasks.Count > 0 ? Task.WhenAll(handleTasks) : Task.CompletedTask;
    }

    private List<Task> HandleCssFiles(DirectoryInfo source, string entryFile, string targetDirName) =>
        string.IsNullOrWhiteSpace(entryFile)
            ? HandleFiles(source, "*.css", targetDirName)
            : [HandleCssImports(source, entryFile, targetDirName)];

    private Task HandleCssImports(DirectoryInfo source, string entryFile, string targetDirName) =>
        Task.Run(
            () =>
            {
                var lines    = File.ReadAllLines(Path.Combine(source.FullName, entryFile), Encoding.UTF8);
                var linesMap = new Dictionary<int, string[]>();

                foreach (var (fileName, index) in lines.Where(l => l.StartsWith("@import"))
                                                       .Select(
                                                           (line, index) =>
                                                           {
                                                               var start = line.IndexOf('"');
                                                               start = start > -1 ? start + 1 : 0;
                                                               var end = line.LastIndexOf('"');
                                                               end = end > start ? end : line.Length - 1;

                                                               return (line[start..end], index);
                                                           }))
                {
                    var importLines = File.ReadAllLines(Path.Combine(source.FullName, fileName), Encoding.UTF8);
                    linesMap.TryAdd(index, importLines);
                }

                var dir    = CreateDirectoryIfNotExists(targetDirName);
                var output = Path.Combine(dir, entryFile);

                using var writer = new StreamWriter(output, append: true);

                foreach (var (index, line) in lines.Index())
                {
                    if (linesMap.TryGetValue(index, out var importLines))
                    {
                        foreach (var importLine in importLines)
                        {
                            writer.WriteLine(importLine);
                        }
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            });

    private List<Task> HandleJsFiles(DirectoryInfo source, string entryFile, string targetDirName)
    {
        const string fileExtension = "*.js";

        if (string.IsNullOrWhiteSpace(entryFile))
        {
            return HandleFiles(source, fileExtension, targetDirName);
        }

        // TODO: Replace later, use this for now
        return HandleFiles(source, fileExtension, targetDirName);
    }

    private List<Task> HandleFiles(DirectoryInfo source, string fileExtension, string targetDirName)
    {
        var files = source.GetFiles(fileExtension, SearchOption.AllDirectories);
        if (files.Length <= 0)
        {
            return [];
        }

        var dir = CreateDirectoryIfNotExists(targetDirName);

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

    private string CreateDirectoryIfNotExists(string subDir)
    {
        var subDirParts = subDir.Split(Path.DirectorySeparatorChar);

        var dir = Path.Combine(Environment.CurrentDirectory, _options.Value.OutputPath);
        Directory.CreateDirectory(dir); // Probably already created in previous step, but to be safe

        foreach (var part in subDirParts)
        {
            dir = Path.Combine(dir, part);
            Directory.CreateDirectory(dir);
        }

        return dir;
    }

    private static bool IsFileOfType(string extension, in string[] extensions, in string entryFile) =>
        extensions.Contains(extension) || entryFile.EndsWith(extension);

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
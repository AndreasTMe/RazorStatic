using Microsoft.Extensions.Options;
using RazorStatic.Abstractions;
using RazorStatic.Configuration;
using RazorStatic.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal sealed partial class StaticContentHandler : IStaticContentHandler
{
    private sealed record FileState(
        FileInfo File,
        string Directory,
        string CommonDirectory,
        bool IsValidCommonDirectory);

    private sealed record CssImportState(FileSystemInfo Source, string EntryFile, string OutputDirectory);

    private readonly IDirectoriesSetup                         _directories;
    private readonly IDirectoriesSetupForStaticContent         _directoriesStaticContent;
    private readonly IOptions<RazorStaticConfigurationOptions> _options;

    public StaticContentHandler(
        IDirectoriesSetup directories,
        IDirectoriesSetupForStaticContent directoriesStaticContent,
        IOptions<RazorStaticConfigurationOptions> options)
    {
        _directories              = directories;
        _directoriesStaticContent = directoriesStaticContent;
        _options                  = options;
    }

    public Task HandleAsync(CancellationToken cancellationToken)
    {
        var tasks       = new List<Task>();
        var projectRoot = _directories.ProjectRoot;

        foreach (var (rootPath, extensions, entryFile) in _directoriesStaticContent)
        {
            if (extensions is not { Length: > 0 } && string.IsNullOrWhiteSpace(entryFile))
                continue;

            var currentRoot      = new DirectoryInfo(Path.Combine(projectRoot, rootPath));
            var indexOfSeparator = rootPath.IndexOf(Path.DirectorySeparatorChar) + 1;
            var targetDirName    = indexOfSeparator != 0 ? rootPath[indexOfSeparator..] : rootPath;
            targetDirName = targetDirName.StartsWith('_') ? targetDirName : "_" + targetDirName;

            // TODO: It's better to have a parser for both CSS and JS files, but that's too much for now
            if (IsFileOfType(".css", extensions, entryFile))
            {
                tasks.AddRange(HandleCssFilesAsync(currentRoot, entryFile, targetDirName, cancellationToken));
            }
            else if (IsFileOfType(".js", extensions, entryFile))
            {
                tasks.AddRange(HandleJsFilesAsync(currentRoot, entryFile, targetDirName, cancellationToken));
            }

            foreach (var extension in extensions.SkipWhile(static e => e.Equals(".css") || e.Equals(".js")))
            {
                tasks.AddRange(HandleFilesAsync(currentRoot, extension, targetDirName, cancellationToken));
            }
        }

        return TaskUtils.RunBatchAsync(tasks, _options.Value.MaxConcurrentFiles);
    }

    private IEnumerable<Task> HandleCssFilesAsync(
        DirectoryInfo source,
        string entryFile,
        string targetDirName,
        CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(entryFile)
            ? HandleFilesAsync(source, ".css", targetDirName, cancellationToken)
            : HandleCssImportsAsync(source, entryFile, targetDirName, cancellationToken);

    private IEnumerable<Task> HandleCssImportsAsync(
        FileSystemInfo source,
        string entryFile,
        string targetDirName,
        CancellationToken cancellationToken)
    {
        yield return Task.Factory.StartNew(
            static state =>
            {
                var taskState = (CssImportState)state!;

                var lines = File.ReadAllLines(
                    Path.Combine(taskState.Source.FullName, taskState.EntryFile),
                    Encoding.UTF8);
                var urls     = new List<string>();
                var linesMap = new Dictionary<int, string>();

                foreach (var (index, line) in lines.Index())
                {
                    // TODO: What about comments, bro? Don't care for now...

                    if (line.StartsWith("@import", StringComparison.Ordinal))
                    {
                        var start = line.IndexOf('"');
                        start = start > -1 ? start + 1 : 0;

                        if (start > 0 && line[..start].Contains("url("))
                        {
                            urls.Add(line);
                            continue;
                        }

                        var end = line.LastIndexOf('"');
                        end = end > start ? end : line.Length - 1;

                        var importText = File.ReadAllText(
                            Path.Combine(taskState.Source.FullName, line[start..end]),
                            Encoding.UTF8);
                        linesMap.TryAdd(index, importText);

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    break;
                }

                var sb = new StringBuilder();
                foreach (var url in urls)
                {
                    sb.AppendLine(url);
                }

                // TODO: CSS obfuscation would be fun, but that's definitely gonna need a parser, indexing, etc.
                foreach (var (index, line) in lines.Index())
                {
                    sb.AppendLine(linesMap.GetValueOrDefault(index, line));
                }

                var output = Path.Combine(taskState.OutputDirectory, taskState.EntryFile);

                // TODO: Maybe split if line is too long? Not sure if it can be a problem.
                File.WriteAllText(output, WhitespaceRegex().Replace(sb.ToString(), " "), Encoding.UTF8);
            },
            new CssImportState(source, entryFile, CreateDirectoryIfNotExists(targetDirName, _options.Value.OutputPath)),
            cancellationToken,
            TaskCreationOptions.DenyChildAttach,
            TaskScheduler.Default);
    }

    private IEnumerable<Task> HandleJsFilesAsync(
        DirectoryInfo source,
        string entryFile,
        string targetDirName,
        CancellationToken cancellationToken)
    {
        const string fileExtension = ".js";

        if (string.IsNullOrWhiteSpace(entryFile))
        {
            return HandleFilesAsync(source, fileExtension, targetDirName, cancellationToken);
        }

        // Handle the following:
        // ------------------------------------------------------------
        // Named import:        import { export1, export2 } from "module-name";
        // Default import:      import defaultExport from "module-name";
        // Namespace import:    import * as name from "module-name";
        // Side effect import:  import "module-name";
        // ------------------------------------------------------------

        // TODO: Replace JS handling later, use default behaviour for now
        return HandleFilesAsync(source, fileExtension, targetDirName, cancellationToken);
    }

    private IEnumerable<Task> HandleFilesAsync(
        DirectoryInfo source,
        string fileExtension,
        string targetDirName,
        CancellationToken cancellationToken)
    {
        if (!fileExtension.StartsWith('*'))
        {
            fileExtension = '*' + fileExtension;
        }

        var files = source.GetFiles(fileExtension, SearchOption.AllDirectories);
        if (files.Length <= 0)
        {
            yield break;
        }

        var directory = CreateDirectoryIfNotExists(targetDirName, _options.Value.OutputPath);

        var commonDirectory        = GetCommonDirectory(source.FullName, files);
        var isValidCommonDirectory = !string.IsNullOrWhiteSpace(commonDirectory);

        foreach (var file in files)
        {
            yield return Task.Factory.StartNew(
                static state =>
                {
                    var taskState = (FileState)state!;

                    var actualDir = taskState.Directory;
                    if (!taskState.IsValidCommonDirectory)
                    {
                        var subDir = taskState.File.DirectoryName
                                         ?.Replace(taskState.CommonDirectory, string.Empty)
                                         .TrimStart(Path.DirectorySeparatorChar)
                                     ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(subDir))
                        {
                            actualDir = Path.Combine(taskState.Directory, subDir);
                            Directory.CreateDirectory(actualDir);
                        }
                    }

                    taskState.File.CopyTo(Path.Combine(actualDir, taskState.File.Name), true);
                },
                new FileState(file, directory, commonDirectory, isValidCommonDirectory),
                cancellationToken,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }
    }

    private static string CreateDirectoryIfNotExists(string subDir, string outputPath)
    {
        var subDirParts = subDir.Split(Path.DirectorySeparatorChar);

        var dir = Path.Combine(Environment.CurrentDirectory, outputPath);
        Directory.CreateDirectory(dir); // Probably already created in previous step, but to be safe

        foreach (var part in subDirParts)
        {
            dir = Path.Combine(dir, part);
            Directory.CreateDirectory(dir);
        }

        return dir;
    }

    private static bool IsFileOfType(string extension, in string[] extensions, in string entryFile) =>
        extensions.Contains(extension) || entryFile.EndsWith(extension, StringComparison.Ordinal);

    private static string GetCommonDirectory(string sourceDirectory, IEnumerable<FileInfo> files)
    {
        var directories = new List<string[]>();

        foreach (var file in files.Where(static f => f.DirectoryName is not null))
        {
            var fileEdited = file.DirectoryName!.Replace(sourceDirectory, string.Empty);
            directories.Add(fileEdited.Split(Path.DirectorySeparatorChar));
        }

        if (directories.Count == 0 || directories[0].Length == 0)
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

            for (var i = 1; i < directories.Count; i++)
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

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
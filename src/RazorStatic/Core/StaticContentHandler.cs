using Microsoft.Extensions.Options;
using RazorStatic.Configuration;
using RazorStatic.Shared;
using RazorStatic.Shared.Attributes;
using RazorStatic.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RazorStatic.Core;

internal sealed class StaticContentHandler : IStaticContentHandler
{
    private readonly IDirectoriesSetup                         _directoriesSetup;
    private readonly IOptions<RazorStaticConfigurationOptions> _options;

    public StaticContentHandler(IDirectoriesSetup directoriesSetup, IOptions<RazorStaticConfigurationOptions> options)
    {
        _directoriesSetup = directoriesSetup;
        _options          = options;
    }

    public async Task HandleAsync()
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

            var source = new DirectoryInfo(_directoriesSetup.Static);

            var finalCopyTasks = new List<Task>();

            var cssCopyTasks = CopyToOutput(source, "*.css", Constants.Static.CssDirectory);
            if (cssCopyTasks.Count > 0)
            {
                finalCopyTasks.AddRange(cssCopyTasks);
            }

            var jsCopyTasks = CopyToOutput(source, "*.js", Constants.Static.JsDirectory);
            if (jsCopyTasks.Count > 0)
            {
                finalCopyTasks.AddRange(finalCopyTasks);
            }

            if (finalCopyTasks.Count > 0)
            {
                await Task.WhenAll(finalCopyTasks).ConfigureAwait(false);
            }
        }

        if (!string.IsNullOrWhiteSpace(_directoriesSetup.Tailwind))
        {
            var tailwindOutDir = Path.Combine(Environment.CurrentDirectory, Constants.Tailwind.Output);
            Debug.Assert(Directory.Exists(tailwindOutDir));

            var tailwindSource    = new DirectoryInfo(tailwindOutDir);
            var tailwindCopyTasks = CopyToOutput(tailwindSource, "*.css", Constants.Static.CssDirectory);
            if (tailwindCopyTasks.Count > 0)
            {
                await Task.WhenAll(tailwindCopyTasks);
            }
        }
    }

    private List<Task> CopyToOutput(DirectoryInfo source, string fileExtension, string targetDirName)
    {
        var files = source.GetFiles(fileExtension, SearchOption.AllDirectories);
        if (files.Length <= 0)
        {
            return [];
        }

        var dir = Path.Combine(Environment.CurrentDirectory, _options.Value.OutputPath, targetDirName);
        Directory.CreateDirectory(dir);

        return files.Select(file => Task.FromResult(file.CopyTo(Path.Combine(dir, file.Name), true)))
                    .Cast<Task>()
                    .ToList();
    }
}
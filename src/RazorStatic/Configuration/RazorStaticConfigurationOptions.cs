using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace RazorStatic.Configuration;

/// <summary>
/// TODO: Documentation
/// </summary>
public sealed class RazorStaticConfigurationOptions
{
    private const string PortErrorMessage       = "A port must be greater than 1024 and less than 65535.";
    private const string OutputPathErrorMessage = "An output path for the static files is required.";

    private int    _port       = 13390;
    private string _outputPath = "out";
    private bool   _isAbsoluteOutputPath;

    private bool _isLocked;

    public int Port
    {
        get => _port;
        set => SetValueIfUnlocked(ref _port, value);
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetValueIfUnlocked(ref _outputPath, value);
    }

    public bool IsAbsoluteOutputPath
    {
        get => _isAbsoluteOutputPath;
        set => SetValueIfUnlocked(ref _isAbsoluteOutputPath, value);
    }

    internal bool ShouldServe { get; private set; }

    internal void AddCommandLineArgs(string[]? args)
    {
        if (args is not { Length: > 0 })
            return;

        var argsDictionary = args.Select(
                                     arg =>
                                     {
                                         var kvp = arg.Split('=');
                                         return new KeyValuePair<string, string?>(
                                             kvp[0],
                                             kvp.Length == 2 ? kvp[1] : null);
                                     })
                                 .ToFrozenDictionary();

        ShouldServe = argsDictionary.TryGetValue("--serve", out var value) && value is null or "true";
    }

    internal void Evaluate()
    {
        if (_isLocked)
            return;

        var messages = new List<string>();

        if (Port is < 1024 or >= 65535) messages.Add(PortErrorMessage);

        EvaluateString(OutputPath, messages, OutputPathErrorMessage);

        if (messages is { Count: > 0 })
            throw new ArgumentException($"RazorStatic configuration problems found: '{string.Join(" ", messages)}'");

        _isLocked = true;
    }

    private static void EvaluateString(string field, List<string> messages, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(field))
            messages.Add(errorMessage);
    }

    private void SetValueIfUnlocked<T>(ref T member, T value)
    {
        if (_isLocked)
            return;

        member = value;
    }
}
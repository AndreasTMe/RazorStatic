﻿using RazorStatic.Shared;
using System;
using System.Threading.Tasks;

namespace RazorStatic.Utilities;

internal sealed class NullPagesStore : IPagesStore
{
    public string RootPath { get; } = string.Empty;

    public Type GetPageType(string filePath) => null!;

    public Task<string> RenderComponentAsync(string filePath) => Task.FromResult(string.Empty);

    public Task<string> RenderLayoutComponentAsync(string filePath, string htmlBody) => Task.FromResult(string.Empty);
}
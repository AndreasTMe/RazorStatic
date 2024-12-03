using RazorStatic.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace RazorStatic.Utilities;

internal sealed class NullPageCollectionsStore : IPageCollectionsStore
{
    public bool TryGetCollection(string filePath, [MaybeNullWhen(false)] out IPageCollectionDefinition collection)
    {
        collection = null;
        return false;
    }

    public string[] GetContentFileDirectories(string key) => [];
}
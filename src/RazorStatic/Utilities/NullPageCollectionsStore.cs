using RazorStatic.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace RazorStatic.Utilities;

internal sealed class NullPageCollectionsStore : IPageCollectionsStore
{
    public bool TryGetCollection(string key, [MaybeNullWhen(false)] out IPageCollectionDefinition collection)
    {
        collection = null;
        return false;
    }
}
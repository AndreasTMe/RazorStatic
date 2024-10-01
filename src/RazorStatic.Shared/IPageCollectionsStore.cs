using System.Diagnostics.CodeAnalysis;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPageCollectionsStore
{
    bool TryGetCollection(string key, [MaybeNullWhen(false)] out IPageCollectionDefinition collection);
}
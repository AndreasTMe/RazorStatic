using System.Diagnostics.CodeAnalysis;

namespace RazorStatic.Abstractions;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface IPageCollectionsStore
{
    bool TryGetCollection(string filePath, [MaybeNullWhen(false)] out IPageCollectionDefinition collection);
}
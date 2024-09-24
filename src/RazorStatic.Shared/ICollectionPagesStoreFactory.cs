using System.Diagnostics.CodeAnalysis;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface ICollectionPagesStoreFactory
{
    bool TryGetCollection(string key, [MaybeNullWhen(false)] out ICollectionPagesStore collection);
}
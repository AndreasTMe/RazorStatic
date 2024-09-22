using System.Diagnostics.CodeAnalysis;

namespace RazorStatic.Shared;

/// <summary>
/// TODO: Documentation
/// </summary>
public interface ICollectionPagesStoreFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="collection"></param>
    /// <returns></returns>
    bool TryGetCollection(string key, [MaybeNullWhen(false)] out ICollectionPagesStore collection);
}
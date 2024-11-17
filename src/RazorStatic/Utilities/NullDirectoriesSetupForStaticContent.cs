using RazorStatic.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RazorStatic.Utilities;

internal sealed class NullDirectoriesSetupForStaticContent : IDirectoriesSetupForStaticContent
{
    public IEnumerator<ValueTuple<string, string[], string>> GetEnumerator() =>
        (IEnumerator<ValueTuple<string, string[], string>>)Array.Empty<ValueTuple<string, string[], string>>()
                                                                .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
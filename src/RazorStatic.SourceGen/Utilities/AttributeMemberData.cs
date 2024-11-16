﻿using System.Collections.Immutable;

namespace RazorStatic.SourceGen.Utilities;

internal readonly record struct AttributeMemberData
{
    public ImmutableDictionary<string, string> Properties { get; }

    public AttributeMemberData(ImmutableDictionary<string, string> properties) => Properties = properties;
}
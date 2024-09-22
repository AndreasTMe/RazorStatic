using System.Collections.Immutable;

namespace RazorStatic.SourceGen.Utilities;

internal record Capture(CsProjProperties Properties,
                        string? AssemblyName = default,
                        ImmutableArray<AttributeClassInfo> ClassInfos = default);
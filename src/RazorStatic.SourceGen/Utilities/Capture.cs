using System.Collections.Immutable;

namespace RazorStatic.SourceGen.Utilities;

internal record Capture(CsProjProperties Properties,
                        string? AssemblyName = default,
                        AttributeMemberData DirectorySetup = default,
                        ImmutableArray<AttributeMemberData> AttributeMembers = default);
using System.Collections.Immutable;

namespace RazorStatic.SourceGen.Utilities;

internal record Capture
{
    public CsProjProperties                    Properties       { get; }
    public string                              AssemblyName     { get; }
    public AttributeMemberData                 DirectorySetup   { get; }
    public ImmutableArray<AttributeMemberData> AttributeMembers { get; }

    public Capture(CsProjProperties properties,
                   string? assemblyName = default,
                   AttributeMemberData directorySetup = default,
                   ImmutableArray<AttributeMemberData> attributeMembers = default)
    {
        Properties       = properties;
        AssemblyName     = !string.IsNullOrWhiteSpace(assemblyName) ? assemblyName! : "";
        DirectorySetup   = directorySetup;
        AttributeMembers = attributeMembers;
    }
}
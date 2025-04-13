using System.Collections.Immutable;

namespace RazorStatic.SourceGen.Utilities;

internal sealed record Capture
{
    public CsProjProperties                    Properties                { get; }
    public string                              AssemblyName              { get; }
    public AttributeMemberData                 DirectorySetup            { get; }
    public ImmutableArray<AttributeMemberData> AttributeMembers          { get; }
    public ImmutableArray<AttributeMemberData> AttributeExtensionMembers { get; set; }

    public Capture(
        CsProjProperties properties,
        string? assemblyName = null,
        AttributeMemberData directorySetup = default,
        ImmutableArray<AttributeMemberData> attributeMembers = default,
        ImmutableArray<AttributeMemberData> attributeExtensionMembers = default)
    {
        Properties                = properties;
        AssemblyName              = !string.IsNullOrWhiteSpace(assemblyName) ? assemblyName! : "";
        DirectorySetup            = directorySetup;
        AttributeMembers          = attributeMembers;
        AttributeExtensionMembers = attributeExtensionMembers;
    }
}
using System;

namespace RazorStatic.Shared.Attributes;

/// <summary>
/// Place this attribute to a partial class that is going to be used by the source generator for creating a map
/// to all the ".razor" components in the "/Pages" directory.
/// </summary>
/// <remarks>
/// <para>The "/Pages" directory is required for this to work.</para>
/// <para>This attribute can be used only once per project.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PagesStoreAttribute : Attribute;
﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RazorStatic.SourceGen.Utilities;
using System.Text;

namespace RazorStatic.SourceGen.Extensions;

internal static class IncrementalGeneratorInitializationContextExtensions
{
    public static void AddDirectoriesSetupAttribute(this IncrementalGeneratorInitializationContext context) =>
        context.RegisterPostInitializationOutput(
            static postInitializationContext =>
                postInitializationContext.AddSource(
                    $"{Constants.Attributes.DirectoriesSetup.Name}Attribute.generated.cs",
                    SourceText.From(
                        $$"""
                          // <auto-generated/>
                          namespace {{Constants.RazorStaticAttributesNamespace}};

                          [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
                          public sealed class {{Constants.Attributes.DirectoriesSetup.Name}}Attribute : System.Attribute
                          {
                          #nullable disable
                              public string {{Constants.Attributes.DirectoriesSetup.Members.Pages}} { get; set; }
                              
                              public string {{Constants.Attributes.DirectoriesSetup.Members.Content}} { get; set; }
                          #nullable enable
                          }
                          """,
                        Encoding.UTF8)));

    public static void AddCollectionDefinitionAttribute(this IncrementalGeneratorInitializationContext context) =>
        context.RegisterPostInitializationOutput(
            static postInitializationContext =>
                postInitializationContext.AddSource(
                    $"{Constants.Attributes.CollectionDefinition.Name}Attribute.generated.cs",
                    SourceText.From(
                        $$"""
                          // <auto-generated/>
                          namespace {{Constants.RazorStaticAttributesNamespace}};

                          [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
                          public sealed class {{Constants.Attributes.CollectionDefinition.Name}}Attribute : System.Attribute
                          {
                          #nullable disable
                              public string {{Constants.Attributes.CollectionDefinition.Members.Key}} { get; set; }
                              
                              public string {{Constants.Attributes.CollectionDefinition.Members.PageRoute}} { get; set; }
                          
                              public string {{Constants.Attributes.CollectionDefinition.Members.ContentDirectory}} { get; set; }
                          
                              public string {{Constants.Attributes.CollectionDefinition.Members.Extension}} { get; set; }
                          #nullable enable
                          }
                          """,
                        Encoding.UTF8)));

    public static void AddStaticContentAttribute(this IncrementalGeneratorInitializationContext context) =>
        context.RegisterPostInitializationOutput(
            static postInitializationContext =>
                postInitializationContext.AddSource(
                    $"{Constants.Attributes.StaticContent.Name}Attribute.generated.cs",
                    SourceText.From(
                        $$"""
                          // <auto-generated/>
                          namespace {{Constants.RazorStaticAttributesNamespace}};

                          [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
                          public sealed class {{Constants.Attributes.StaticContent.Name}}Attribute : System.Attribute
                          {
                          #nullable disable
                              public string {{Constants.Attributes.StaticContent.Members.RootPath}} { get; set; }
                          
                              public string[] {{Constants.Attributes.StaticContent.Members.Extensions}} { get; set; }
                          
                              public string {{Constants.Attributes.StaticContent.Members.EntryFile}} { get; set; }
                          #nullable enable
                          }
                          """,
                        Encoding.UTF8)));

    public static IncrementalValuesProvider<AttributeMembers> GetSyntaxProvider(
        this IncrementalGeneratorInitializationContext context,
        string attributeName) =>
        context.SyntaxProvider.ForAttributeWithMetadataName(
            $"{Constants.RazorStaticAttributesNamespace}.{attributeName}Attribute",
            (node, _) => node.IsTargetAttributeNode(attributeName),
            (ctx, _) => ctx.TargetNode.GetAttributeMembers(ctx.SemanticModel, attributeName));
}
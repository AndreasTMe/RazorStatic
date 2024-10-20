using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RazorStatic.Shared.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace RazorStatic.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class DirectoriesSetupSyntaxAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "Rst1000";

    private const string Category = "RazorStatic";

    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resources.Rst1000Title),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString Description =
        new LocalizableResourceString(
            nameof(Resources.Rst1000Description),
            Resources.ResourceManager,
            typeof(Resources));

    private static readonly LocalizableString MessageFormat =
        new LocalizableResourceString(
            nameof(Resources.Rst1000MessageFormat),
            Resources.ResourceManager,
            typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        customTags: "CompilationEnd");

    private static readonly string DirectoriesSetup = nameof(DirectoriesSetupAttribute)
        .Replace(nameof(Attribute), string.Empty);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(
            analysisContext =>
            {
                var diagnostics = new ConcurrentBag<Diagnostic>();

                analysisContext.RegisterSyntaxNodeAction(
                    syntaxContext =>
                    {
                        if (syntaxContext.Node is not AttributeSyntax attributeSyntax)
                            return;

                        if (!attributeSyntax.Name.ToString().Equals(DirectoriesSetup))
                            return;

                        diagnostics.Add(Diagnostic.Create(Rule, attributeSyntax.GetLocation()));
                    },
                    SyntaxKind.Attribute);

                analysisContext.RegisterCompilationEndAction(
                    compilationEndContext =>
                    {
                        if (diagnostics.Count <= 1)
                            return;

                        foreach (var diagnostic in diagnostics)
                            compilationEndContext.ReportDiagnostic(diagnostic);
                    });
            });
    }
}
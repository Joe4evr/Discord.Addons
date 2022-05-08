#if NET6_0_OR_GREATER
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Discord.Addons.MpGame.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class GameStateOneOfAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "MG0002";
    private const string Title = "Use more than 1 value in RequireGameStateOneOfAttribute";
    private const string MessageFormat = "Use more than 1 value in RequireGameStateOneOfAttribute";
    private const string Description = "Use more than 1 value in RequireGameStateOneOfAttribute.";
    private const string Category = "API Usage";

#pragma warning disable RS2008 // Enable analyzer release tracking
    private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
#pragma warning restore RS2008 // Enable analyzer release tracking
    private static readonly Type _stateProviderType = typeof(ISimpleStateProvider<>);
    private static readonly Type _moduleBaseType = typeof(MpGameModuleBase<,,>);
    private static readonly Type _attributeType =  _moduleBaseType.GetNestedType("RequireGameStateOneOfAttribute`1", BindingFlags.NonPublic)!;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.Attribute);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AttributeSyntax attributeSyntax)
            return;

        var attrTypeInfo = context.SemanticModel.GetTypeInfo(attributeSyntax);
        if (attrTypeInfo.Type is null
            || attrTypeInfo.Type.OriginalDefinition.MetadataName != _attributeType.Name)
            return;

        var containingClassSyntax = context.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (containingClassSyntax is null)
            return;

        var containingClass = context.SemanticModel.GetDeclaredSymbol(containingClassSyntax);
        if (containingClass is null || !containingClass.IsMpGameModuleClass(_moduleBaseType, out var mpGameModuleSymbol))
            return;

        if (attributeSyntax.ArgumentList?.Arguments.Count is null or 0 or 1)
        {
            var diag = Diagnostic.Create(_rule, attributeSyntax.GetLocation());
            context.ReportDiagnostic(diag);
            return;
        }
    }
}
#endif

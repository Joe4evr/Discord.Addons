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
internal sealed class GameStateProviderAnalyzer : DiagnosticAnalyzer
{
    internal const string DiagnosticId = "MG0001";
    private const string Title = "Game type should implement ISimpleStateProvider";
    private const string MessageFormat = "Game type '{0}' does not implement 'ISimpleStateProvider<{1}>'";
    private const string Description = "Game type has to implement ISimpleStateProvider to make use of this precondition.";
    private const string Category = "API Usage";

#pragma warning disable RS2008 // Enable analyzer release tracking
    private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
#pragma warning restore RS2008 // Enable analyzer release tracking
    private static readonly Type _stateProviderType = typeof(ISimpleStateProvider<>);
    private static readonly Type _moduleBaseType = typeof(MpGameModuleBase<,,>);
    private static readonly Type[] _attributeTypes = new[]
    {
        _moduleBaseType.GetNestedType("RequireGameStateAttribute`1", BindingFlags.NonPublic)!,
        _moduleBaseType.GetNestedType("RequireGameStateOneOfAttribute`1", BindingFlags.NonPublic)!,
    };

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

        var attrTypeSymbol = (context.SemanticModel.GetTypeInfo(attributeSyntax).Type as INamedTypeSymbol);
        if (!_attributeTypes.Any(a => attrTypeSymbol?.OriginalDefinition.MetadataName == a.Name))
            return;

        var containingClassSyntax = context.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (containingClassSyntax is null)
            return;

        var containingClass = context.SemanticModel.GetDeclaredSymbol(containingClassSyntax);
        if (containingClass is null || !containingClass.IsMpGameModuleClass(_moduleBaseType, out var mpGameModuleSymbol))
            return;

        var stateType = context.SemanticModel.GetTypeInfo(((GenericNameSyntax)attributeSyntax.Name).TypeArgumentList.Arguments[0]).Type!;
        var gameTypeSymbol = mpGameModuleSymbol.TypeArguments[1];
        var stateProvSymbol = gameTypeSymbol.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.MetadataName == _stateProviderType.Name);
        if (stateProvSymbol is null || !SymbolEqualityComparer.Default.Equals(stateProvSymbol.TypeArguments[0], stateType))
        {
            var diag = Diagnostic.Create(_rule, attributeSyntax.GetLocation(), gameTypeSymbol.Name, stateType.Name);
            context.ReportDiagnostic(diag);
            return;
        }
    }
}
#endif

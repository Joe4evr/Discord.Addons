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

namespace Discord.Addons.MpGame.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class GameStateProviderAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "MG0001";
        private const string Title = "Game type should implement ISimpleStateProvider.";
        private const string MessageFormat = "Game type '{0}' does not implement 'ISimpleStateProvider<{1}>'.";
        private const string Description = "Game type has to implement ISimpleStateProvider to make use of this precondition.";
        private const string Category = "API Usage";

        private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly Type _stateProviderType = typeof(ISimpleStateProvider<>);
        private static readonly Type _moduleBaseType = typeof(MpGameModuleBase<,,>);
        private static readonly Type _attributeType = _moduleBaseType.GetNestedType("RequireGameStateAttribute", BindingFlags.NonPublic)!;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public GameStateProviderAnalyzer() { }

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
            if (attrTypeInfo.Type.OriginalDefinition.MetadataName != _attributeType.Name)
                return;

            var containingClassSyntax = context.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var containingClass = context.SemanticModel.GetDeclaredSymbol(containingClassSyntax);
            if (!IsMpGameModuleClass(containingClass, out var mpGameModuleSymbol))
                return;

            var gameTypeSymbol = mpGameModuleSymbol.TypeArguments[1];
            var stateProvSymbol = gameTypeSymbol.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.MetadataName == _stateProviderType.Name);
            if (stateProvSymbol == null)
            {
                var diag = Diagnostic.Create(_rule, attributeSyntax.GetLocation(), gameTypeSymbol.Name, "TState");
                context.ReportDiagnostic(diag);
                return;
            }

            var tstateTypeSymbol = stateProvSymbol.TypeArguments[0];
        }

        private static bool IsMpGameModuleClass(INamedTypeSymbol typeSymbol,
            [NotNullWhen(true)] out INamedTypeSymbol? mpGameModuleSymbol)
        {
            for (var t = typeSymbol; t is not null; t = t.BaseType)
            {
                if (t.OriginalDefinition.MetadataName == _moduleBaseType.Name)
                {
                    mpGameModuleSymbol = t;
                    return true;
                }
            }

            mpGameModuleSymbol = null;
            return false;
        }
    }
}
#endif

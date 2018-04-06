using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Discord.Addons.MpGame.Collections;

namespace Discord.Addons.MpGame.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class BufferStrategySetterAnalyzer : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "MPG0001";
        internal const string Title = "Restrict BufferStrategy setting";
        internal const string MessageFormat = "Do not set the BufferStrategy outside of the constructor.";
        internal const string Description = "Do not set the BufferStrategy outside of the constructor.";
        internal const string Category = "API Usage";
        internal const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, Severity, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleAssignmentExpression);
        }

        private static readonly Type _pileType = typeof(Pile<>);
        private static readonly Type _bufferStratType = typeof(IBufferStrategy<>);

        private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is AssignmentExpressionSyntax assignment))
                return; //technically never false, but let's not make assumptions

            var ctor = assignment.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (ctor != null)
                return; //we inside a ctor, this analyzer doesn't care anymore

            var symbolInfo = context.SemanticModel.GetSymbolInfo(assignment.Left);
            if (!(symbolInfo.Symbol is IPropertySymbol symbol))
                return; //assignment wasn't a property, get outta here

            if (symbol.Type.IsOrDerivesFromType(_bufferStratType))
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }
    }
}

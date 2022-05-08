#if NET6_0_OR_GREATER
using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace MpGame.Tests.Analyzers
{
    public sealed class GameStateProviderAnalyzerTests : DiagnosticVerifier
    {
        [Theory]
        [InlineData(SourceTexts.MockGameNoStateDiag)]
        [InlineData(SourceTexts.MockGameWrongStateDiag)]
        public async Task VerifyDiagnostic(string gameSourceText)
        {
            var expected = new DiagnosticResult[]
            {
                new()
                {
                    Id = "MG0001",
                    Message = "Game type 'MockGame' does not implement 'ISimpleStateProvider<MockGameState>'",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation(path: "Test3.cs", line: 15, column: 6)
                    }
                },
                new()
                {
                    Id = "MG0001",
                    Message = "Game type 'MockGame' does not implement 'ISimpleStateProvider<MockGameState>'",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation(path: "Test3.cs", line: 17, column: 6)
                    }
                }
            };

            await VerifyCSharpDiagnostic(sources: new[]
            {
                gameSourceText,
                SourceTexts.MockService,
                SourceTexts.MockGameState,
                SourceTexts.MockModule
            }, expected);
        }

        [Fact]
        public async Task VerifyNoDiagnostic()
        {
            await VerifyCSharpDiagnostic(sources: new[]
            {
                SourceTexts.MockGameValid,
                SourceTexts.MockService,
                SourceTexts.MockGameState,
                SourceTexts.MockModule
            }, Array.Empty<DiagnosticResult>());
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => (Activator.CreateInstance(
                assemblyName: "Discord.Addons.MpGame",
                typeName: "Discord.Addons.MpGame.Analyzers.GameStateProviderAnalyzer")?.Unwrap() as DiagnosticAnalyzer)!;
    }
}
#endif

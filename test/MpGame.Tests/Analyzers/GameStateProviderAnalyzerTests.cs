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
        [Fact]
        public async Task VerifyDiagnostic()
        {
            var expected = new DiagnosticResult[]
            {
                new()
                {
                    Id = "MG0001",
                    Message = "Game type 'MockGame' does not implement 'ISimpleStateProvider<MockGameState>'.",
                    Severity = DiagnosticSeverity.Warning,
                    Locations = new[]
                    {
                        new DiagnosticResultLocation(path: "Test3.cs", line: 8, column: 68)
                    }
                }
            };

            await VerifyCSharpDiagnostic(sources: new[]
            {
                SourceTexts.MockGame,
                SourceTexts.MockService,
                SourceTexts.MockGameState,
                SourceTexts.MockModuleWithDiag
            }, expected);
        }


        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
            => (Activator.CreateInstance(
                assemblyName: "Discord.Addons.MpGame",
                typeName: "Discord.Addons.MpGame.Analyzers.GameStateProviderAnalyzer")?.Unwrap() as DiagnosticAnalyzer)!;
    }
}
#endif

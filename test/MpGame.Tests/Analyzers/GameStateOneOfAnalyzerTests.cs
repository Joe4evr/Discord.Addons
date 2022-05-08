#if NET6_0_OR_GREATER
using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace MpGame.Tests.Analyzers;

public sealed class GameStateOneOfAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public async Task VerifyDiagnostic()
    {
        var expected = new DiagnosticResult[]
        {
            new()
            {
                Id = "MG0002",
                Message = "Use more than 1 value in RequireGameStateOneOfAttribute",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(path: "Test3.cs", line: 15, column: 6)
                }
            },
            new()
            {
                Id = "MG0002",
                Message = "Use more than 1 value in RequireGameStateOneOfAttribute",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(path: "Test3.cs", line: 17, column: 6)
                }
            },
            new()
            {
                Id = "MG0002",
                Message = "Use more than 1 value in RequireGameStateOneOfAttribute",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[]
                {
                    new DiagnosticResultLocation(path: "Test3.cs", line: 19, column: 6)
                }
            }
        };

        await VerifyCSharpDiagnostic(sources: new[]
        {
            SourceTexts.MockGameValid,
            SourceTexts.MockService,
            SourceTexts.MockGameState,
            SourceTexts.MockModuleOneOfError
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
            typeName: "Discord.Addons.MpGame.Analyzers.GameStateOneOfAnalyzer")?.Unwrap() as DiagnosticAnalyzer)!;
}
#endif

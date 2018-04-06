using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Discord.Addons;
using Discord.Addons.MpGame.Collections;
using TestHelper;
using Xunit;

using Analyzer = Discord.Addons.MpGame.Analyzers.BufferStrategySetterAnalyzer;

namespace MpGame.Tests.AnalyzerTests
{
    public sealed class BufferStrategyAnalyzerTests : DiagnosticVerifier
    {
        [Fact]
        public void VerifyDiagnosticOutsideCtor()
        {
            var code = @"using Discord.Addons;
using Discord.Addons.MpGame.Collections;

namespace Test
{
    public sealed class TestCard { }
    public sealed class TestPile : Pile<TestCard>
    {
        void M(IBufferStrategy<TestCard> bufferStrategy)
        {
            BufferStrategy = bufferStrategy;
            I = 42;
        }

        public int I { get; private set; }

        public override bool CanBrowse { get; }
        public override bool CanClear { get; }
        public override bool CanCut { get; }
        public override bool CanDraw { get; }
        public override bool CanInsert { get; }
        public override bool CanPeek { get; }
        public override bool CanPut { get; }
        public override bool CanPutBottom { get; }
        public override bool CanShuffle { get; }
        public override bool CanTake { get; }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Analyzer.DiagnosticId,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line: 11, column: 13) },
                Message = Analyzer.MessageFormat,
                Severity = Analyzer.Severity
            };
            VerifyCSharpDiagnostic(code, expected);
        }

        [Fact]
        public void VerifyNoDiagnosticInsideCtor()
        {
            var code = @"using Discord.Addons;
using Discord.Addons.MpGame.Collections;

namespace Test
{
    public sealed class TestCard { }
    public sealed class TestPile : Pile<TestCard>
    {
        public TestPile(IBufferStrategy<TestCard> bufferStrategy)
        {
            BufferStrategy = bufferStrategy;
            I = 42;
        }

        public int I { get; }

        public override bool CanBrowse { get; }
        public override bool CanClear { get; }
        public override bool CanCut { get; }
        public override bool CanDraw { get; }
        public override bool CanInsert { get; }
        public override bool CanPeek { get; }
        public override bool CanPut { get; }
        public override bool CanPutBottom { get; }
        public override bool CanShuffle { get; }
        public override bool CanTake { get; }
    }
}";

            VerifyCSharpDiagnostic(code, Array.Empty<DiagnosticResult>());
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new Analyzer();
    }

    public sealed class TestCard { }
    public sealed class TestPile : Pile<TestCard>
    {
        void M(IBufferStrategy<TestCard> bufferStrategy)
        {
            BufferStrategy = bufferStrategy;
        }

        public override bool CanBrowse { get; }
        public override bool CanClear { get; }
        public override bool CanCut { get; }
        public override bool CanDraw { get; }
        public override bool CanInsert { get; }
        public override bool CanPeek { get; }
        public override bool CanPut { get; }
        public override bool CanPutBottom { get; }
        public override bool CanShuffle { get; }
        public override bool CanTake { get; }
    }
}

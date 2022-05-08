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

internal static class AnalyzerExtensions
{
    internal static bool IsMpGameModuleClass(this INamedTypeSymbol typeSymbol,
        Type baseType, [NotNullWhen(true)] out INamedTypeSymbol? mpGameModuleSymbol)
    {
        for (var t = typeSymbol; t is not null; t = t.BaseType)
        {
            if (t.OriginalDefinition.MetadataName == baseType.Name)
            {
                mpGameModuleSymbol = t;
                return true;
            }
        }

        mpGameModuleSymbol = null;
        return false;
    }
}

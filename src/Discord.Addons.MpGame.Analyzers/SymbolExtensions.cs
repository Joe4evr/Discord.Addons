using System;
using Microsoft.CodeAnalysis;
using Discord.Addons.MpGame.Collections;

namespace Discord.Addons.MpGame.Analyzers
{
    internal static class SymbolExtensions
    {
        private static readonly string _pileName = typeof(Pile<>).Name;

        public static bool DerivesFromPile(this ITypeSymbol symbol)
        {
            for (var bType = symbol.BaseType; bType != null; bType = bType.BaseType)
            {
                if (bType.MetadataName == _pileName)
                    return true;
            }
            return false;
        }
    }
}

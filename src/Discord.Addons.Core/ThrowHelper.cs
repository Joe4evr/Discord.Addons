using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Discord.Addons.Core
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn, DebuggerStepThrough]
        internal static void ThrowInvalidOp(string msg)
            => throw new InvalidOperationException(message: msg);

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn, DebuggerStepThrough]
        internal static void ThrowArgNull(string argname)
            => throw new ArgumentNullException(paramName: argname);

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn, DebuggerStepThrough]
        internal static void ThrowArgOutOfRange(string msg, string argname)
            => throw new ArgumentOutOfRangeException(message: msg, paramName: argname);

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn, DebuggerStepThrough]
        internal static void ThrowIndexOutOfRange(string msg)
            => throw new IndexOutOfRangeException(message: msg);
    }
}

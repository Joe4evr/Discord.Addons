using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Discord.Addons.Core
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining), DebuggerStepThrough]
        internal static void ThrowInvalidOp(string msg)
            => throw new InvalidOperationException(message: msg);

        [MethodImpl(MethodImplOptions.NoInlining), DebuggerStepThrough]
        internal static void ThrowIfArgNull<T>([EnsuresNotNull] T arg, string argname)
            where T : class
        {
            if (arg == null)
                throw new ArgumentNullException(paramName: argname);
        }

        [MethodImpl(MethodImplOptions.NoInlining), DebuggerStepThrough]
        internal static void ThrowArgOutOfRange(string msg, string argname)
            => throw new ArgumentOutOfRangeException(message: msg, paramName: argname);

        [MethodImpl(MethodImplOptions.NoInlining), DebuggerStepThrough]
        internal static void ThrowIndexOutOfRange(string msg)
            => throw new IndexOutOfRangeException(message: msg);

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
        internal sealed class EnsuresNotNullAttribute : Attribute { }
    }
}

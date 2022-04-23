using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Discord.Addons.SimpleAudio;

internal static class AllocExtensions
{
    // Based on code by @DaZombieKiller.
    internal static unsafe ArraySegment<byte> AllocateAlignedBuffer<TAlignFor>(int requestedSize)
        where TAlignFor : unmanaged
    {
        // Calculate a slightly larger buffer size to compensate
        // for platforms that require address-aligned access (like ARM).
        var alignment = AlignmentOf<TAlignFor>();
        var actualSize = requestedSize + alignment - 1;

        // Ensuring the buffer is never moved by GC should save some cycles.
        // Also save cycles from skipping zero-init since we don't read before writing.
        byte[] buffer = GC.AllocateUninitializedArray<byte>(actualSize, pinned: true);
        var address = (nint)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(buffer));
        var offset = (int)(((address + alignment - 1) & (~(alignment - 1))) - address);

        // If 'offset' ended up non-zero,
        // clearing out the leading bytes
        // *does* seem like a good idea.
        buffer.AsSpan()[0..offset].Clear();

        return new(buffer, offset, actualSize - offset);
    }

    private static unsafe int AlignmentOf<T>()
        where T : unmanaged
    {
        return sizeof(AlignmentHelper<T>) - sizeof(T);
    }

    private readonly struct AlignmentHelper<T>
        where T : unmanaged
    {
        internal readonly byte _padding;
        internal readonly T _value;
    }
}

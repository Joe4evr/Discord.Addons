using System;
using Google.Protobuf;

namespace Discord.Addons.MpGame
{
    internal static class Extensions
    {
        public static Guid ToGuid(this ByteString bytes)
            => new Guid(bytes.Span);

        public static ByteString ToByteString(this Guid guid)
        {
            Span<byte> buffer = stackalloc byte[16]; //sizeof(Guid) == 16
            guid.TryWriteBytes(buffer);
            return ByteString.CopyFrom(buffer);
        }
    }
}

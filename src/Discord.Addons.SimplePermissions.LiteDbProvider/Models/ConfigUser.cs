using System;
using LiteDB;

namespace Discord.Addons.SimplePermissions.LiteDbProvider
{
    /// <summary> </summary>
    public class ConfigUser
    {
        /// <summary> </summary>
        [BsonId(true)]
        public int Id { get; set; }

        /// <summary> </summary>
        public ulong UserId { get; set; }

        public override string ToString() => $"{base.ToString()} ({UserId})";
    }
}

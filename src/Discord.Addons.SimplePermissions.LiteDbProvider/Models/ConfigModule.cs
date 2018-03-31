using System;
using LiteDB;

namespace Discord.Addons.SimplePermissions.LiteDbProvider
{
    /// <summary> </summary>
    public sealed class ConfigModule
    {
        /// <summary> </summary>
        [BsonId(true)]
        public int Id { get; set; }

        /// <summary> </summary>
        public string ModuleName { get; set; }
    }
}

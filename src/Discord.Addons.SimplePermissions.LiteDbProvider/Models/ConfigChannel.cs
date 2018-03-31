using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteDB;

namespace Discord.Addons.SimplePermissions.LiteDbProvider
{
    /// <summary> </summary>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        /// <summary> </summary>
        [BsonId(true)]
        public int Id { get; set; }

        /// <summary> </summary>
        public ulong ChannelId { get; set; }

        /// <summary> </summary>
        [BsonRef("specialUsers")]
        public IList<TUser> SpecialUsers { get; set; }

        /// <summary> </summary>
        [BsonRef("modules")]
        public IList<ConfigModule> WhiteListedModules { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteDB;

namespace Discord.Addons.SimplePermissions.LiteDbProvider
{
    /// <summary> </summary>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigGuild<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        /// <summary> </summary>
        [BsonId(true)]
        public int Id { get; set; }

        /// <summary> </summary>
        public ulong GuildId { get; set; }

        /// <summary> </summary>
        public ulong ModRole { get; set; }

        /// <summary> </summary>
        public ulong AdminRole { get; set; }

        /// <summary> </summary>
        public bool UseFancyHelp { get; set; }

        /// <summary> </summary>
        public bool HidePermCommands { get; set; }

        /// <summary> </summary>
        [BsonRef("channels")]
        public IList<TChannel> Channels { get; set; }

        /// <summary> </summary>
        [BsonRef("modules")]
        public IList<ConfigModule> WhiteListedModules { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigGuild<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser    : ConfigUser
    {
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong GuildId { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong ModRole { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong AdminRole { get; set; }

        /// <summary> </summary>
        public ICollection<TChannel> Channels { get; set; } = new List<TChannel>();

        /// <summary> </summary>
        public ICollection<TUser> Users { get; set; } = new List<TUser>();

        /// <summary> </summary>
        public ICollection<ConfigModule> WhiteListedModules { get; set; } = new List<ConfigModule>();
    }

    public class ConfigGuild<TUser> : ConfigGuild<ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser { }

    public class ConfigGuild : ConfigGuild<ConfigChannel, ConfigUser> { }
}

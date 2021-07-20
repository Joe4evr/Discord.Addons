using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

#nullable disable warnings
namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    [NotMapped]
    public abstract class ConfigGuild<TGuild, TChannel, TUser>
        where TGuild : ConfigGuild<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        /// <summary> </summary>
        public IEnumerable<GuildModule<TGuild, TChannel,TUser>> WhiteListedModules { get; set; }

        internal ConfigGuild()
        {
        }
    }

    /// <summary> </summary>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigGuild<TChannel, TUser> : ConfigGuild<ConfigGuild<TChannel, TUser>, TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        /// <summary> </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary> </summary>
        public ulong GuildId { get; set; }

        /// <summary> </summary>
        public ulong ModRoleId { get; set; }

        /// <summary> </summary>
        public ulong AdminRoleId { get; set; }

        /// <summary> </summary>
        public bool UseFancyHelp { get; set; }

        /// <summary> </summary>
        public bool HidePermCommands { get; set; }

        /// <summary> </summary>
        public ICollection<TChannel> Channels { get; set; }
    }

    /// <summary> </summary>
    public class ConfigGuild<TUser> : ConfigGuild<ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser
    {
    }

    /// <summary> </summary>
    public class ConfigGuild : ConfigGuild<ConfigUser>
    {
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

#nullable disable warnings
namespace Discord.Addons.SimplePermissions
{
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
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        [NotMapped]
        public ulong GuildId
        {
            get => unchecked((ulong)_gid);
            set => _gid = unchecked((long)value);
        }

        /// <summary> </summary>
        [NotMapped]
        public ulong ModRoleId
        {
            get => unchecked((ulong)_mid);
            set => _mid = unchecked((long)value);
        }

        /// <summary> </summary>
        [NotMapped]
        public ulong AdminRoleId
        {
            get => unchecked((ulong)_aid);
            set => _aid = unchecked((long)value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        internal long _gid;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        internal long _mid;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        internal long _aid;

        public bool UseFancyHelp { get; set; }

        public bool HidePermCommands { get; set; }

        /// <summary> </summary>
        public ICollection<TChannel> Channels { get; set; }
    }

    public class ConfigGuild<TUser> : ConfigGuild<ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser
    {
    }

    public class ConfigGuild : ConfigGuild<ConfigUser>
    {
    }
}

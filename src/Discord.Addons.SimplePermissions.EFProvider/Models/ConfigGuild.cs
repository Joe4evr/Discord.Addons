using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigGuild<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser    : ConfigUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        //[NotMapped]
        public ulong GuildId
        {
            get => unchecked((ulong)_gid);
            set => _gid = unchecked((long)value);
        }

        /// <summary> </summary>
        ///[NotMapped]
        public ulong ModRole
        {
            get => unchecked((ulong)_mid);
            set => _mid = unchecked((long)value);
        }

        /// <summary> </summary>
        ///[NotMapped]
        public ulong AdminRole
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

        /// <summary> </summary>
        public ICollection<TUser> Users { get; set; }

        /// <summary> </summary>
        public ICollection<ConfigModule> WhiteListedModules { get; set; }
    }

    public class ConfigGuild<TUser> : ConfigGuild<ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser
    {
    }

    public class ConfigGuild : ConfigGuild<ConfigChannel, ConfigUser>
    {
    }
}

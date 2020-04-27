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
    public abstract class ConfigChannel<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        /// <summary> </summary>
        public IEnumerable<ChannelUser<TChannel, TUser>> SpecialUsers { get; set; }

        /// <summary> </summary>
        public IEnumerable<ChannelModule<TChannel, TUser>> WhiteListedModules { get; set; }

        internal ConfigChannel()
        {
        }
    }

    /// <summary> </summary>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigChannel<TUser> : ConfigChannel<ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        [NotMapped]
        public ulong ChannelId
        {
            get => unchecked((ulong)_cid);
            set => _cid = unchecked((long)value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        internal long _cid;
    }

    public class ConfigChannel : ConfigChannel<ConfigUser>
    {
    }
}

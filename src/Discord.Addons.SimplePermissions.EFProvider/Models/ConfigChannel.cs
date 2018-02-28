using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        ///[NotMapped]
        public ulong ChannelId
        {
            get => unchecked((ulong)_cid);
            set => _cid = unchecked((long)value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Browsable(false)]
        internal long _cid;

        /// <summary> </summary>
        public ICollection<TUser> SpecialUsers { get; set; }

        /// <summary> </summary>
        public ICollection<ConfigModule> WhiteListedModules { get; set; }
    }

    public class ConfigChannel : ConfigChannel<ConfigUser>
    {
    }
}

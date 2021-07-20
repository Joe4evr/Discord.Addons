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
        /// <summary> </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary> </summary>
        public ulong ChannelId { get; set; }
    }

    /// <summary> </summary>
    public class ConfigChannel : ConfigChannel<ConfigUser>
    {
    }
}

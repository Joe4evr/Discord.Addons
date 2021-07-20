using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings
namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public sealed class ChannelUser<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        /// <summary> </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary> </summary>
        public TChannel Channel { get; set; }

        /// <summary> </summary>
        public TUser User { get; set; }
    }
}

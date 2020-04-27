using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings
namespace Discord.Addons.SimplePermissions
{
    public sealed class ChannelUser<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public TChannel Channel { get; set; }

        public TUser User { get; set; }
    }
}

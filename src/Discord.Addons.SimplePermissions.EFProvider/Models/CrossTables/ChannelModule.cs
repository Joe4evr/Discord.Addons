using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    public sealed class ChannelModule<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public TChannel Channel { get; set; }

        public ConfigModule Module { get; set; }
    }
}

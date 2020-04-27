using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings
namespace Discord.Addons.SimplePermissions
{
    public sealed class GuildModule<TGuild, TChannel, TUser>
        where TGuild : ConfigGuild<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public TGuild Guild { get; set; }

        public ConfigModule Module { get; set; }
    }
}

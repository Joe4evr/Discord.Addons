using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings
namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public sealed class GuildModule<TGuild, TChannel, TUser>
        where TGuild : ConfigGuild<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        /// <summary> </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary> </summary>
        public TGuild Guild { get; set; }

        /// <summary> </summary>
        public ConfigModule Module { get; set; }
    }
}

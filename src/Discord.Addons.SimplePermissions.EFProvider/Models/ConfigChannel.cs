using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigChannel<TUser>
        where TUser : ConfigUser
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong ChannelId { get; set; }

        /// <summary> </summary>
        public ICollection<TUser> SpecialUsers { get; set; }

        /// <summary> </summary>
        public ICollection<ConfigModule> WhiteListedModules { get; set; }
    }

    public class ConfigChannel : ConfigChannel<ConfigUser> { }

}

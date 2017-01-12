using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public class ConfigChannel
    {
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong ChannelId { get; set; }

        /// <summary> </summary>
        public ICollection<ConfigUser> SpecialUsers { get; set; } = new List<ConfigUser>();

        /// <summary> </summary>
        public ICollection<ConfigModule> WhiteListedModules { get; set; } = new List<ConfigModule>();
    }
}

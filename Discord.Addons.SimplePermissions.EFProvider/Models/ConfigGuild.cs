using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public class ConfigGuild
    {
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong GuildId { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong ModRole { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong AdminRole { get; set; }

        /// <summary> </summary>
        public ICollection<ConfigChannel> Channels { get; set; } = new List<ConfigChannel>();

        /// <summary> </summary>
        public ICollection<ConfigUser> Users { get; set; } = new List<ConfigUser>();
    }
}

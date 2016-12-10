using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    public class ConfigGuild
    {
        internal ConfigGuild()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ulong ModRole { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ulong AdminRole { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ConfigChannel> Channels { get; set; } = new List<ConfigChannel>();
    }
}

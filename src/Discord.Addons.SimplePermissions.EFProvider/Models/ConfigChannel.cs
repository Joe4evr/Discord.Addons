using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    public class ConfigChannel
    {
        internal ConfigChannel()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        [Key]
        public ulong ChannelId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public HashSet<ulong> SpecialUsers { get; set; } = new HashSet<ulong>();

        /// <summary>
        /// 
        /// </summary>
        public HashSet<string> WhiteListedModules { get; set; } = new HashSet<string>();
    }
}

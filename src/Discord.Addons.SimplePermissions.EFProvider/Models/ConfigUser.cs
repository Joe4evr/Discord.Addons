using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public class ConfigUser
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong UserId { get; set; }

        /// <summary> </summary>
        [Column(TypeName = "BIGINT UNSIGNED")]
        public ulong GuildId { get; set; }
    }
}

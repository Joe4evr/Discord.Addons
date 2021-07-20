﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable warnings
namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public sealed class ConfigModule
    {
        /// <summary> </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary> </summary>
        public string ModuleName { get; set; }
    }
}

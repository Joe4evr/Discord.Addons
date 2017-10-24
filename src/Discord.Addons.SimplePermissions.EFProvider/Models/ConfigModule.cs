using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public sealed class ConfigModule
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        public string ModuleName { get; set; }
    }
}

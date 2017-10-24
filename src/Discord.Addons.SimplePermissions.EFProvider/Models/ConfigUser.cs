using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    public class ConfigUser
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        //[Column(TypeName = "BIGINT")]
        [NotMapped]
        public ulong UserId
        {
            get => unchecked((ulong)_uid);
            set => _uid = unchecked((long)value);
        }

        private long _uid;
    }
}

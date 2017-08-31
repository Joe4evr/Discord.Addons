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
        //[Column(TypeName = "BIGINT")]
        [NotMapped]
        public ulong UserId
        {
            get => Converter.LongToUlong(_uid);
            set => _uid = Converter.UlongToLong(value);
        }

        private long _uid;
    }
}

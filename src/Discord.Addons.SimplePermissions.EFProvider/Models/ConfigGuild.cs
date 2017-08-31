using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> </summary>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class ConfigGuild<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>
        where TUser    : ConfigUser
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        /// <summary> </summary>
        public int Id { get; set; }

        /// <summary> </summary>
        [NotMapped]
        public ulong GuildId
        {
            get => Converter.LongToUlong(_gid);
            set => _gid = Converter.UlongToLong(value);
        }

        /// <summary> </summary>
        [NotMapped]
        public ulong ModRole
        {
            get => Converter.LongToUlong(_mid);
            set => _mid = Converter.UlongToLong(value);
        }

        /// <summary> </summary>
        [NotMapped]
        public ulong AdminRole
        {
            get => Converter.LongToUlong(_aid);
            set => _aid = Converter.UlongToLong(value);
        }

        private long _gid;
        private long _mid;
        private long _aid;

        public bool UseFancyHelp { get; set; }

        public bool HidePermCommands { get; set; }

        /// <summary> </summary>
        public ICollection<TChannel> Channels { get; set; }

        /// <summary> </summary>
        public ICollection<TUser> Users { get; set; }

        /// <summary> </summary>
        public ICollection<ConfigModule> WhiteListedModules { get; set; }
    }

    public class ConfigGuild<TUser> : ConfigGuild<ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser
    {
    }

    public class ConfigGuild : ConfigGuild<ConfigChannel, ConfigUser>
    {
    }
}

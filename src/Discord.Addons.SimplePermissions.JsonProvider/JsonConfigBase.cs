using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of <see cref="IPermissionConfig"/> using
    /// in-memory collection as a backing store, suitable for
    /// serialization to and from JSON. </summary>
    public partial class JsonConfigBase : ISetPath
    {
        internal IEnumerable<ModuleInfo> Modules { private get; set; }

        /// <summary> Gets whether fancy help messages are
        /// enabled in a specified guild. </summary>
        public Dictionary<ulong, bool> UseFancyHelps { get; set; }

        /// <summary> Gets the ID of the group that is considered
        /// the Admin role in a specified guild. </summary>
        public Dictionary<ulong, ulong> GuildAdminRole { get; set; }

        /// <summary> Gets the ID of the group that is considered
        /// the Moderator role in a specified guild. </summary>
        public Dictionary<ulong, ulong> GuildModRole { get; set; }

        /// <summary> Gets the list of modules that are
        /// whitelisted in a specified channel. </summary>
        public Dictionary<ulong, HashSet<string>> ChannelModuleWhitelist { get; set; }

        /// <summary> Gets the list of modules that are
        /// whitelisted in a specified guild. </summary>
        public Dictionary<ulong, HashSet<string>> GuildModuleWhitelist { get; set; }

        /// <summary> Gets the users that are allowed to use
        /// commands marked <see cref="MinimumPermission.Special"/>
        /// in a channel. </summary>
        public Dictionary<ulong, HashSet<ulong>> SpecialPermissionUsersList { get; set; }

        public Dictionary<ulong, bool> HidePermCommandValues { get; set; }

        private FileInfo _path;

        FileInfo ISetPath.Path { set => _path = value; }

        public void Save()
            => File.WriteAllText(_path.FullName, JsonConvert.SerializeObject(this, Formatting.Indented));

        void IDisposable.Dispose() { }
    }

    internal interface ISetPath
    {
        FileInfo Path { set; }
    }
}

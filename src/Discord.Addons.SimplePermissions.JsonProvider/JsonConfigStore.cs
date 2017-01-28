using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Basic implementation of <see cref="IConfigStore{TConfig}"/> that stores and loads
    /// a configuration object as JSON on disk using JSON.NET. </summary>
    public sealed class JsonConfigStore<TConfig> : IConfigStore<TConfig>
        where TConfig : JsonConfigBase, new()
    {
        private readonly string _jsonPath;
        private readonly TConfig _config;

        /// <summary> Initializes a new instance of <see cref="JsonConfigStore{TConfig}"/>.
        /// Will create a new file if it does not exist. </summary>
        /// <param name="path">Path to the JSON file.</param>
        public JsonConfigStore(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path))
                File.WriteAllText(path, JsonConvert.SerializeObject(new TConfig(), Formatting.Indented));

            _jsonPath = path;
            _config = JsonConvert.DeserializeObject<TConfig>(File.ReadAllText(_jsonPath));

            if (_config.ChannelModuleWhitelist == null)
                _config.ChannelModuleWhitelist = new Dictionary<ulong, HashSet<string>>();

            if (_config.GuildModuleWhitelist == null)
                _config.GuildModuleWhitelist = new Dictionary<ulong, HashSet<string>>();

            if (_config.GuildAdminRole == null)
                _config.GuildAdminRole = new Dictionary<ulong, ulong>();

            if (_config.GuildModRole == null)
                _config.GuildModRole = new Dictionary<ulong, ulong>();

            if (_config.SpecialPermissionUsersList == null)
                _config.SpecialPermissionUsersList = new Dictionary<ulong, HashSet<ulong>>();
        }

        /// <summary> Load the configuration from disk. </summary>
        /// <returns>The configuration object.</returns>
        public TConfig Load()
        {
            return _config;
        }

        /// <summary> Saves the configuration object to disk. </summary>
        public void Save()
        {
            File.WriteAllText(_jsonPath, JsonConvert.SerializeObject(_config, Formatting.Indented));
        }
    }
}

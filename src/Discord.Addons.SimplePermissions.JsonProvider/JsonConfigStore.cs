using System;
using System.Collections.Generic;
using System.IO;
using Discord.Commands;
using Newtonsoft.Json;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Basic implementation of <see cref="IConfigStore{TConfig}"/> that stores and loads
    /// a configuration object as JSON on disk using JSON.NET. </summary>
    public sealed class JsonConfigStore<TConfig> : IConfigStore<TConfig>
        where TConfig : JsonConfigBase, new()
    {
        private readonly string _jsonPath;
        private readonly CommandService _commands;

        /// <summary> Initializes a new instance of <see cref="JsonConfigStore{TConfig}"/>.
        /// Will create a new file if it does not exist. </summary>
        /// <param name="path">Path to the JSON file.</param>
        public JsonConfigStore(string path, CommandService commands)
        {
            _jsonPath = path ?? throw new ArgumentNullException(nameof(path));
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));

            if (!File.Exists(path))
                File.WriteAllText(path, JsonConvert.SerializeObject(new TConfig(), Formatting.Indented));

            using (var config = Load())
            {
                if (config.ChannelModuleWhitelist == null)
                {
                    config.ChannelModuleWhitelist = new Dictionary<ulong, HashSet<string>>();
                    config.Save();
                }

                if (config.GuildModuleWhitelist == null)
                {
                    config.GuildModuleWhitelist = new Dictionary<ulong, HashSet<string>>();
                    config.Save();
                }

                if (config.GuildAdminRole == null)
                {
                    config.GuildAdminRole = new Dictionary<ulong, ulong>();
                    config.Save();
                }

                if (config.GuildModRole == null)
                {
                    config.GuildModRole = new Dictionary<ulong, ulong>();
                    config.Save();
                }

                if (config.SpecialPermissionUsersList == null)
                {
                    config.SpecialPermissionUsersList = new Dictionary<ulong, HashSet<ulong>>();
                    config.Save();
                }

                if (config.UseFancyHelps == null)
                {
                    config.UseFancyHelps = new Dictionary<ulong, bool>();
                    config.Save();
                }
            }
        }

        /// <summary> Load the configuration from disk. </summary>
        /// <returns>The configuration object.</returns>
        public TConfig Load()
        {
            var config = JsonConvert.DeserializeObject<TConfig>(File.ReadAllText(_jsonPath));
            config.Modules = _commands.Modules;
            (config as ISetPath).SetPath(_jsonPath);
            return config;
        }
    }
}

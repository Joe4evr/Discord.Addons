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
        private readonly FileInfo _jsonPath;
        private readonly CommandService _commands;

        /// <summary> Initializes a new instance of <see cref="JsonConfigStore{TConfig}"/>.
        /// Will create a new file if it does not exist. </summary>
        /// <param name="path">Path to the JSON file.</param>
        public JsonConfigStore(string path, CommandService commands)
        {
            try
            {
                _jsonPath = new FileInfo(Path.GetFullPath(path));

                if (!_jsonPath.Exists)
                    File.WriteAllText(_jsonPath.FullName, JsonConvert.SerializeObject(new TConfig(), Formatting.Indented));
            }
            catch (Exception ex)
            {
                throw new AggregateException(message: $"Parameter '{nameof(path)}' must be a valid file path.", innerException: ex);
            }

            _commands = commands ?? throw new ArgumentNullException(nameof(commands));

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
        public TConfig Load(IServiceProvider services = null)
        {
            var config = JsonConvert.DeserializeObject<TConfig>(File.ReadAllText(_jsonPath.FullName));
            config.Modules = _commands.Modules;
            (config as ISetPath).Path = _jsonPath;
            return config;
        }
    }
}

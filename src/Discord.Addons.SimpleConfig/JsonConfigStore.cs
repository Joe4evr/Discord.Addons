using System;
using System.IO;
using Newtonsoft.Json;

namespace Discord.Addons.SimpleConfig
{
    /// <summary>
    /// Basic implementation of <see cref="IConfigStore{TConfig}"/> that stores and loads
    /// a configuration object as JSON on disk using JSON.NET.
    /// </summary>
    /// <typeparam name="TConfig"></typeparam>
    public sealed class JsonConfigStore<TConfig> : IConfigStore<TConfig> where TConfig : IConfig
    {
        private readonly string _jsonPath;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonConfigStore{TConfig}"/>.
        /// Will create a new file if it does not exist.
        /// </summary>
        /// <param name="path">Path to the JSON file.</param>
        public JsonConfigStore(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) File.WriteAllText(path, "{}");

            _jsonPath = path;
        }

        /// <summary>
        /// Load the configuration from disk.
        /// </summary>
        /// <returns>The configuration object.</returns>
        public TConfig Load()
        {
            return JsonConvert.DeserializeObject<TConfig>(File.ReadAllText(_jsonPath));
        }

        /// <summary>
        /// Saves the configuration object to disk.
        /// </summary>
        /// <param name="config">The configuration object.</param>
        public void Save(TConfig config)
        {
            File.WriteAllText(_jsonPath, JsonConvert.SerializeObject(config));
        }
    }
}

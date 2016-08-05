using System;
using System.IO;
using Newtonsoft.Json;

namespace Discord.Addons.SimpleConfig
{
    /// <summary>
    /// Basic implementation of <see cref="IConfigStore{TConfig}"/> that stores and loads
    /// an <see cref="IConfig"/> as JSON on disk using JSON.NET.
    /// </summary>
    public sealed class JsonConfigStore : IConfigStore<IConfig>
    {
        private readonly string _jsonPath;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonConfigStore"/>.
        /// Will create a new file if it does not exist.
        /// </summary>
        /// <param name="path">Path to the JSON file.</param>
        public JsonConfigStore(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) File.WriteAllText(path, "{}");

            _jsonPath = path;
        }

        public IConfig Load()
        {
            return JsonConvert.DeserializeObject<IConfig>(File.ReadAllText(_jsonPath));
        }

        public void Save(IConfig config)
        {
            File.WriteAllText(_jsonPath, JsonConvert.SerializeObject(config));
        }
    }
}

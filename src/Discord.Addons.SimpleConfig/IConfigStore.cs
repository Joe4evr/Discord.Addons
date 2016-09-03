namespace Discord.Addons.SimpleConfig
{
    /// <summary>
    /// Defines a contract that stores and loads an <see cref="IConfig"/>.
    /// </summary>
    /// <typeparam name="TConfig">Type of the config object.</typeparam>
    public interface IConfigStore<TConfig>
        where TConfig : IConfig
    {
        /// <summary>
        /// Load the configuration into an object.
        /// </summary>
        /// <returns>The config object.</returns>
        TConfig Load();

        /// <summary>
        /// Save the config object to a persistent location.
        /// </summary>
        void Save(TConfig config);
    }
}

using System;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Defines a contract that stores and loads an <see cref="IPermissionConfig"/>. </summary>
    /// <typeparam name="TConfig">Type of the config object.</typeparam>
    public interface IConfigStore<out TConfig>
        where TConfig : IPermissionConfig
    {
        /// <summary> Load the configuration object. </summary>
        /// <returns>The config object.</returns>
        TConfig Load();
    }
}

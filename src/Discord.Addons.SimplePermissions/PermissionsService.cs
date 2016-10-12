using System;
using System.Threading.Tasks;
using Discord.Addons.SimpleConfig;
using Discord.WebSocket;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PermissionsService
    {
        private readonly IConfigStore<IPermissionConfig> _configStore;
        internal IPermissionConfig Config { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configstore"></param>
        /// <param name="client"></param>
        public PermissionsService(
            IConfigStore<IPermissionConfig> configstore,
            DiscordSocketClient client)
        {
            if (configstore == null) throw new ArgumentNullException(nameof(configstore));
            if (client == null) throw new ArgumentNullException(nameof(client));

            Config = configstore.Load();
            _configStore = configstore;

            client.GuildAvailable += async guild =>
            {
                foreach (var chan in await guild.GetTextChannelsAsync())
                {
                    if (Config.ChannelModuleWhitelist[chan.Id].Add(nameof(PermissionsModule)))
                    {
                        _configStore.Save(Config);
                        //Console.WriteLine($"{DateTime.Now}: ");
                    }
                }
            };
            client.ChannelCreated += chan =>
            {
                var tChan = chan as ITextChannel;
                if (tChan != null && Config.ChannelModuleWhitelist[tChan.Id].Add(nameof(PermissionsModule)))
                {
                    _configStore.Save(Config);
                    Console.WriteLine($"{DateTime.Now}: Added permission management to {tChan.Name}.");
                }
                return Task.CompletedTask;
            };
            client.ChannelDestroyed += chan =>
            {
                var tChan = chan as ITextChannel;
                if (tChan != null && Config.ChannelModuleWhitelist[chan.Id].Remove(nameof(PermissionsModule)))
                {
                    _configStore.Save(Config);
                    Console.WriteLine($"{DateTime.Now}: Removed permission management from {tChan.Name}.");
                }
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveConfig() => _configStore.Save(Config);
    }
}

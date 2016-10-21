using System;
using System.Collections.Generic;
using System.Linq;
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
        internal readonly IConfigStore<IPermissionConfig> ConfigStore;
        internal readonly IPermissionConfig Config;

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
            ConfigStore = configstore;

            client.GuildAvailable += async guild =>
            {
                if (!Config.GuildAdminRole.ContainsKey(guild.Id))
                {
                    Config.GuildAdminRole[guild.Id] = 0;
                    ConfigStore.Save();
                }
                if (!Config.GuildModRole.ContainsKey(guild.Id))
                {
                    Config.GuildModRole[guild.Id] = 0;
                    ConfigStore.Save();
                }

                foreach (var chan in await guild.GetTextChannelsAsync())
                {
                    if (await CanReadAndWrite(chan))
                    {
                        if (!Config.ChannelModuleWhitelist.ContainsKey(chan.Id))
                        {
                            Config.ChannelModuleWhitelist[chan.Id] = new HashSet<string>();
                            ConfigStore.Save();
                        }
                        if (!Config.SpecialPermissionUsersList.ContainsKey(chan.Id))
                        {
                            Config.SpecialPermissionUsersList[chan.Id] = new HashSet<ulong>();
                            ConfigStore.Save();
                        }
                        if (Config.ChannelModuleWhitelist[chan.Id].Add(PermissionsModule.permModuleName))
                        {
                            AddPermissionsModule(chan);
                        }
                    }
                }
            };
            client.ChannelCreated += async chan =>
            {
                var mChan = chan as IMessageChannel;
                if (mChan != null && (await CanReadAndWrite(mChan)))
                {
                    if (!Config.ChannelModuleWhitelist.ContainsKey(chan.Id))
                    {
                        Config.ChannelModuleWhitelist[chan.Id] = new HashSet<string>();
                    }

                    AddPermissionsModule(mChan);
                }
            };
            client.ChannelDestroyed += chan =>
            {
                var mChan = chan as IMessageChannel;
                if (mChan != null)
                {
                    RemoveChannel(mChan);
                }
                return Task.CompletedTask;
            };
            client.ChannelUpdated += async (before, after) =>
            {
                var mChan = after as IMessageChannel;
                if (mChan != null && (await CanReadAndWrite(mChan)))
                {
                    AddPermissionsModule(mChan);
                }
                else if (Config.ChannelModuleWhitelist.ContainsKey(after.Id))
                {
                    RemovePermissionsModule(mChan);
                }
            };
        }

        private void RemoveChannel(IMessageChannel channel)
        {
            if (Config.ChannelModuleWhitelist.ContainsKey(channel.Id))
            {
                Config.ChannelModuleWhitelist.Remove(channel.Id);
            }
        }

        private void RemovePermissionsModule(IMessageChannel channel)
        {
            if (Config.ChannelModuleWhitelist[channel.Id].Remove(PermissionsModule.permModuleName))
            {
                ConfigStore.Save();
                Console.WriteLine($"{DateTime.Now}: Removed permission management from {channel.Name}.");
            }
        }

        private void AddPermissionsModule(IMessageChannel channel)
        {
            if (Config.ChannelModuleWhitelist[channel.Id].Add(PermissionsModule.permModuleName))
            {
                ConfigStore.Save();
                Console.WriteLine($"{DateTime.Now}: Added permission management to {channel.Name}.");
            }
        }

        private async Task<bool> CanReadAndWrite(IMessageChannel channel)
        {
            var tChan = channel as ITextChannel;
            if (tChan != null)
            {
                var guild = tChan.Guild;
                var client = await guild.GetCurrentUserAsync();

                var clientPerms = tChan.PermissionOverwrites.Resolve(client).ToList();
                return !(clientPerms.Any(perm => perm.Permissions.ReadMessages == PermValue.Deny)
                    || clientPerms.Any(perm => perm.Permissions.SendMessages == PermValue.Deny));
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveConfig() => ConfigStore.Save();
    }
}

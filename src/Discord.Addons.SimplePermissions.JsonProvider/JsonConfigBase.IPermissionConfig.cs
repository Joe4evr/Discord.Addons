using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    public partial class JsonConfigBase : IPermissionConfig
    {
        Task IPermissionConfig.SetFancyHelpValue(IGuild guild, bool value)
        {
            UseFancyHelps[guild.Id] = value;
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.GetFancyHelpValue(IGuild guild)
        {
            return Task.FromResult(UseFancyHelps[guild.Id]);
        }

        async Task IPermissionConfig.AddNewGuild(IGuild guild)
        {
            if (!GuildAdminRole.ContainsKey(guild.Id))
            {
                GuildAdminRole[guild.Id] = 0ul;
            }
            if (!GuildModRole.ContainsKey(guild.Id))
            {
                GuildModRole[guild.Id] = 0ul;
            }
            if (!GuildModuleWhitelist.ContainsKey(guild.Id))
            {
                GuildModuleWhitelist[guild.Id] = new HashSet<string>();
            }
            if (!UseFancyHelps.ContainsKey(guild.Id))
            {
                UseFancyHelps[guild.Id] = false;
            }

            foreach (var channel in await guild.GetTextChannelsAsync())
            {
                if (!ChannelModuleWhitelist.ContainsKey(channel.Id))
                {
                    ChannelModuleWhitelist[channel.Id] = new HashSet<string>();
                }
                if (!SpecialPermissionUsersList.ContainsKey(channel.Id))
                {
                    SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>();
                }
            }
            await (this as IPermissionConfig).WhitelistModuleGuild(guild, PermissionsModule.PermModuleName).ConfigureAwait(false);
        }

        Task IPermissionConfig.AddChannel(IChannel channel)
        {
            if (channel is IGuildChannel gc)
            {
                if (!ChannelModuleWhitelist.ContainsKey(gc.Id))
                {
                    ChannelModuleWhitelist[gc.Id] = new HashSet<string>();
                }
                if (!SpecialPermissionUsersList.ContainsKey(gc.Id))
                {
                    SpecialPermissionUsersList[gc.Id] = new HashSet<ulong>();
                }
            }
            return Task.CompletedTask;
        }

        Task IPermissionConfig.RemoveChannel(IChannel channel)
        {
            if (ChannelModuleWhitelist.ContainsKey(channel.Id))
            {
                ChannelModuleWhitelist.Remove(channel.Id);
            }
            if (SpecialPermissionUsersList.ContainsKey(channel.Id))
            {
                SpecialPermissionUsersList.Remove(channel.Id);
            }

            return Task.CompletedTask;
        }

        ulong IPermissionConfig.GetGuildAdminRole(IGuild guild)
        {
            return GuildAdminRole[guild.Id];
        }

        ulong IPermissionConfig.GetGuildModRole(IGuild guild)
        {
            return GuildModRole[guild.Id];
        }

        Task<bool> IPermissionConfig.SetGuildAdminRole(IGuild guild, IRole role)
        {
            GuildAdminRole[guild.Id] = role.Id;
            return Task.FromResult(true);
        }

        Task<bool> IPermissionConfig.SetGuildModRole(IGuild guild, IRole role)
        {
            GuildModRole[guild.Id] = role.Id;
            return Task.FromResult(true);
        }

        IEnumerable<ModuleInfo> IPermissionConfig.GetChannelModuleWhitelist(IChannel channel)
        {
            return Modules.Where(m => ChannelModuleWhitelist[channel.Id].Contains(m.Name));
        }

        IEnumerable<ModuleInfo> IPermissionConfig.GetGuildModuleWhitelist(IGuild guild)
        {
            return Modules.Where(m => GuildModuleWhitelist[guild.Id].Contains(m.Name));
        }

        Task<bool> IPermissionConfig.WhitelistModule(IChannel channel, string moduleName)
        {
            return Task.FromResult(ChannelModuleWhitelist[channel.Id].Add(moduleName));
        }

        Task<bool> IPermissionConfig.BlacklistModule(IChannel channel, string moduleName)
        {
            return Task.FromResult(ChannelModuleWhitelist[channel.Id].Remove(moduleName));
        }

        Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, string moduleName)
        {
            return Task.FromResult(GuildModuleWhitelist[guild.Id].Add(moduleName));
        }

        Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, string moduleName)
        {
            return Task.FromResult(GuildModuleWhitelist[guild.Id].Remove(moduleName));
        }

        IEnumerable<ulong> IPermissionConfig.GetSpecialPermissionUsersList(IChannel channel)
        {
            return SpecialPermissionUsersList[channel.Id];
        }

        Task IPermissionConfig.AddUser(IGuildUser user)
        {
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.AddSpecialUser(IChannel channel, IGuildUser user)
        {

            return Task.FromResult(SpecialPermissionUsersList[channel.Id].Add(user.Id));
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(IChannel channel, IGuildUser user)
        {
            return Task.FromResult(SpecialPermissionUsersList[channel.Id].Remove(user.Id));
        }

        Task IPermissionConfig.SetHidePermCommands(IGuild guild, bool newValue)
        {
            HidePermCommandValues[guild.Id] = newValue;
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.GetHidePermCommands(IGuild guild)
        {
            return Task.FromResult(HidePermCommandValues[guild.Id]);
        }
    }
}

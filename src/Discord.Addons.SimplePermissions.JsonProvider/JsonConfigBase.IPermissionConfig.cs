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
        }

        Task IPermissionConfig.AddChannel(ITextChannel channel)
        {
            if (!ChannelModuleWhitelist.ContainsKey(channel.Id))
            {
                ChannelModuleWhitelist[channel.Id] = new HashSet<string>();
            }
            if (!SpecialPermissionUsersList.ContainsKey(channel.Id))
            {
                SpecialPermissionUsersList[channel.Id] = new HashSet<ulong>();
            }
            return Task.CompletedTask;
        }

        Task IPermissionConfig.RemoveChannel(ITextChannel channel)
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

        IRole IPermissionConfig.GetGuildAdminRole(IGuild guild)
        {
            return guild.GetRole(GuildAdminRole[guild.Id]);
        }

        IRole IPermissionConfig.GetGuildModRole(IGuild guild)
        {
            return guild.GetRole(GuildModRole[guild.Id]);
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

        IEnumerable<ModuleInfo> IPermissionConfig.GetChannelModuleWhitelist(ITextChannel channel)
        {
            return Modules.Where(m => ChannelModuleWhitelist[channel.Id].Contains(m.Name));
        }

        IEnumerable<ModuleInfo> IPermissionConfig.GetGuildModuleWhitelist(IGuild guild)
        {
            return Modules.Where(m => GuildModuleWhitelist[guild.Id].Contains(m.Name));
        }

        Task<bool> IPermissionConfig.WhitelistModule(ITextChannel channel, ModuleInfo module)
        {
            return Task.FromResult(ChannelModuleWhitelist[channel.Id].Add(module.Name));
        }

        Task<bool> IPermissionConfig.BlacklistModule(ITextChannel channel, ModuleInfo module)
        {
            return Task.FromResult(ChannelModuleWhitelist[channel.Id].Remove(module.Name));
        }

        Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, ModuleInfo module)
        {
            return Task.FromResult(GuildModuleWhitelist[guild.Id].Add(module.Name));
        }

        Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, ModuleInfo module)
        {
            return Task.FromResult(GuildModuleWhitelist[guild.Id].Remove(module.Name));
        }

        IEnumerable<IGuildUser> IPermissionConfig.GetSpecialPermissionUsersList(ITextChannel channel)
        {
            return SpecialPermissionUsersList[channel.Id].Select(id => channel.Guild.GetUserAsync(id).Result);
        }

        Task IPermissionConfig.AddUser(IGuildUser user)
        {
            return Task.CompletedTask;
        }

        Task<bool> IPermissionConfig.AddSpecialUser(ITextChannel channel, IGuildUser user)
        {
            return Task.FromResult(SpecialPermissionUsersList[channel.Id].Add(user.Id));
        }

        Task<bool> IPermissionConfig.RemoveSpecialUser(ITextChannel channel, IGuildUser user)
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

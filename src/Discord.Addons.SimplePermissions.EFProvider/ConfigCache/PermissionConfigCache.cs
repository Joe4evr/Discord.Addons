//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Threading.Tasks;
//using Discord.Commands;
//using Microsoft.EntityFrameworkCore;

//namespace Discord.Addons.SimplePermissions
//{
//    public partial class EFConfigStore<TContext, TGuild, TChannel, TUser>
//    {
//        private sealed class InMemoryConfig : EFBaseConfigContext<TGuild, TChannel, TUser>, IPermissionConfig
//        {
//            public InMemoryConfig(CommandService commandService)
//                : base(BuildOptions(), commandService)
//            {
//            }

//            internal void Synchronize(TContext sourceContext)
//            {
//                Guilds.UpdateRange(sourceContext.Guilds);
//                Channels.UpdateRange(sourceContext.Channels);
//                Users.UpdateRange(sourceContext.Users);
//                Modules.UpdateRange(sourceContext.Modules);
//                ChannelUsers.UpdateRange(sourceContext.ChannelUsers);
//                ChannelModules.UpdateRange(sourceContext.ChannelModules);
//                GuildModules.UpdateRange(sourceContext.GuildModules);

//                SaveChanges();
//            }

//            private static DbContextOptions BuildOptions()
//            {
//                return new DbContextOptionsBuilder()
//                    .UseInMemoryDatabase(nameof(InMemoryConfig))
//                    .Options;
//            }


//            //writing operations
//            //ALL of these should throw
//            Task IPermissionConfig.AddNewGuild(IGuild guild)
//            {
//                throw new NotImplementedException();
//            }

//            Task IPermissionConfig.AddChannel(ITextChannel channel)
//            {
//                throw new NotImplementedException();
//            }

//            Task IPermissionConfig.RemoveChannel(ITextChannel channel)
//            {
//                throw new NotImplementedException();
//            }

//            Task<bool> IPermissionConfig.SetGuildAdminRole(IGuild guild, IRole role)
//            {
//                throw new NotImplementedException();
//            }

//            Task<bool> IPermissionConfig.SetGuildModRole(IGuild guild, IRole role)
//            {
//                throw new NotImplementedException();
//            }

//            Task<bool> IPermissionConfig.WhitelistModule(ITextChannel channel, ModuleInfo module)
//            {
//                throw new NotImplementedException();
//            }

//            Task<bool> IPermissionConfig.BlacklistModule(ITextChannel channel, ModuleInfo module)
//            {
//                throw new NotImplementedException();
//            }

//            Task<bool> IPermissionConfig.WhitelistModuleGuild(IGuild guild, ModuleInfo module)
//            {
//                throw new NotImplementedException();
//            }

//            Task<bool> IPermissionConfig.BlacklistModuleGuild(IGuild guild, ModuleInfo module)
//            {
//                throw new NotImplementedException();
//            }

//            Task IPermissionConfig.AddUser(IGuildUser user)
//            {
//                throw new NotImplementedException();
//            }

//            Task<bool> IPermissionConfig.AddSpecialUser(ITextChannel channel, IGuildUser user)
//            {
//                throw new NotImplementedException();
//            }

//            Task<bool> IPermissionConfig.RemoveSpecialUser(ITextChannel channel, IGuildUser user)
//            {
//                throw new NotImplementedException();
//            }

//            Task IPermissionConfig.SetFancyHelpValue(IGuild guild, bool newValue)
//            {
//                throw new NotImplementedException();
//            }

//            Task IPermissionConfig.SetHidePermCommands(IGuild guild, bool newValue)
//            {
//                throw new NotImplementedException();
//            }

//            // don't dispose this instance
//            public override void Dispose() { }
//        }
//    }
//}

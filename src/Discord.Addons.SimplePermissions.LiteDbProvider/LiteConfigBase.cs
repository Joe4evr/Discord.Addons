using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LiteDB;
using Discord.Commands;
using Discord.Addons.SimplePermissions.LiteDbProvider;

namespace Discord.Addons.SimplePermissions
{
    public partial class LiteConfigBase<TGuild, TChannel, TUser> : LiteRepository
        where TGuild   : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser    : ConfigUser, new()
    {
        public LiteQueryable<TGuild> Guilds { get; }

        public LiteQueryable<TChannel> Channels { get; }

        public LiteQueryable<TUser> Users { get; }

        public LiteQueryable<ConfigModule> Modules { get; }

        internal IReadOnlyDictionary<string, ModuleInfo> ModuleInfos { private get; set; }

        protected LiteConfigBase(ConnectionString connectionString, BsonMapper mapper)
            : base(connectionString, mapper)
        {
            Guilds   = Query<TGuild>()
                .Include(g => g.Channels)
                .Include(g => g.WhiteListedModules);

            Channels = Query<TChannel>()
                .Include(c => c.SpecialUsers)
                .Include(c => c.WhiteListedModules);

            Users    = Query<TUser>();
            Modules  = Query<ConfigModule>();
        }

        /// <summary> </summary>
        protected virtual Task OnGuildAdd(TGuild configGuild, IGuild guild)
        {
            return Task.CompletedTask;
        }

        /// <summary> </summary>
        protected virtual Task OnChannelAdd(TChannel configChannel, ITextChannel channel)
        {
            return Task.CompletedTask;
        }

        /// <summary> </summary>
        protected virtual Task OnUserAdd(TUser configUser, IGuildUser user)
        {
            return Task.CompletedTask;
        }

        public void Save() { }
    }

    public class LiteConfigBase<TChannel, TUser> : LiteConfigBase<ConfigGuild<TChannel, TUser>, TChannel, TUser>
        where TChannel : ConfigChannel<TUser>, new()
        where TUser : ConfigUser, new()
    {
        protected LiteConfigBase(ConnectionString connectionString, BsonMapper mapper)
            : base(connectionString, mapper)
        {
        }
    }

    public class LiteConfigBase<TUser> : LiteConfigBase<ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser, new()
    {
        protected LiteConfigBase(ConnectionString connectionString, BsonMapper mapper)
            : base(connectionString, mapper)
        {
        }
    }

    public class LiteConfigBase : LiteConfigBase<ConfigUser>
    {
        protected LiteConfigBase(ConnectionString connectionString, BsonMapper mapper)
            : base(connectionString, mapper)
        {
        }
    }
}

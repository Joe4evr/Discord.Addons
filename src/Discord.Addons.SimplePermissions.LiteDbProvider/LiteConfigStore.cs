using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using LiteDB;
using Discord.Commands;
using Discord.Addons.Core;
using Discord.Addons.SimplePermissions.LiteDbProvider;

namespace Discord.Addons.SimplePermissions
{
    public class LiteConfigStore<TConfig, TGuild, TChannel, TUser> : IConfigStore<TConfig>
        where TConfig  : LiteConfigBase<TGuild, TChannel, TUser>
        where TGuild   : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser    : ConfigUser, new()
    {
        private readonly ConnectionString _connectionString;
        private readonly CommandService _commands;
        private readonly BsonMapper _mapper;

        private readonly Func<LogMessage, Task> _logger;

        public LiteConfigStore(
            ConnectionString connectionString,
            CommandService commands,
            BsonMapper mapper = null,
            Func<LogMessage, Task> logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _mapper = mapper ?? BsonMapper.Global;
            _logger = logger ?? Extensions.NoOpLogger;

            _mapper.RegisterType<ulong>(
                serialize: (ul => new BsonValue((long)ul)),
                deserialize: (val => (ulong)val.AsInt64));
        }

        public TConfig Load(IServiceProvider services)
        {
            var ctx = (TConfig)ActivatorUtilities.CreateInstance(services, typeof(TConfig), _connectionString, _mapper);
            ctx.ModuleInfos = _commands.Modules.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
            return ctx;
        }
    }

    public class LiteConfigStore<TConfig, TChannel, TUser> : LiteConfigStore<TConfig, ConfigGuild<TChannel, TUser>, TChannel, TUser>
        where TConfig : LiteConfigBase<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>, new()
        where TUser : ConfigUser, new()
    {
        public LiteConfigStore(
            ConnectionString connectionString,
            CommandService commands,
            BsonMapper mapper = null,
            Func<LogMessage, Task> logger = null)
            : base(connectionString, commands, mapper, logger)
        {
        }
    }

    public class LiteConfigStore<TConfig, TUser> : LiteConfigStore<TConfig, ConfigChannel<TUser>, TUser>
        where TConfig : LiteConfigBase<TUser>
        where TUser : ConfigUser, new()
    {
        public LiteConfigStore(
            ConnectionString connectionString,
            CommandService commands,
            BsonMapper mapper = null,
            Func<LogMessage, Task> logger = null)
            : base(connectionString, commands, mapper, logger)
        {
        }
    }

    public class LiteConfigStore<TConfig> : LiteConfigStore<TConfig, ConfigUser>
        where TConfig : LiteConfigBase
    {
        public LiteConfigStore(
            ConnectionString connectionString,
            CommandService commands,
            BsonMapper mapper = null,
            Func<LogMessage, Task> logger = null)
            : base(connectionString, commands, mapper, logger)
        {
        }
    }
}

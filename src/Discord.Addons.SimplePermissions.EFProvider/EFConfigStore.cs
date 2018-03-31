using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.Addons.Core;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <typeparam name="TGuild">The Guild configuration type.</typeparam>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public partial class EFConfigStore<TContext, TGuild, TChannel, TUser> : IConfigStore<TContext>
        where TContext : EFBaseConfigContext<TGuild, TChannel, TUser>
        where TGuild   : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser    : ConfigUser, new()
    {
        private readonly CommandService _commands;

        private readonly Func<LogMessage, Task> _logger;

        /// <summary> Initializes a new instance of <see cref="EFConfigStore{TContext}"/>. </summary>
        public EFConfigStore(
            CommandService commands,
            Func<LogMessage, Task> logger = null)
        {
            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
            _logger = logger ?? Extensions.NoOpLogger;
        }

        /// <summary> Loads an instance of the DB Context. </summary>
        public TContext Load(IServiceProvider services)
        {
            var ctx = services.CreateScope().ServiceProvider.GetService<TContext>();
            ctx.ModuleInfos = _commands.Modules.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
            return ctx;
        }
    }

    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    /// using the default Guild type. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class EFConfigStore<TContext, TChannel, TUser> : EFConfigStore<TContext, ConfigGuild<TChannel, TUser>, TChannel, TUser>
        where TContext : EFBaseConfigContext<TChannel, TUser>
        where TChannel : ConfigChannel<TUser>, new()
        where TUser : ConfigUser, new()
    {
        public EFConfigStore(
            CommandService commands,
            Func<LogMessage, Task> logger = null)
            : base(commands, logger)
        {
        }
    }

    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    /// using the default Guild and Channel types. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class EFConfigStore<TContext, TUser> : EFConfigStore<TContext, ConfigChannel<TUser>, TUser>
        where TContext : EFBaseConfigContext<TUser>
        where TUser : ConfigUser, new()
    {
        public EFConfigStore(
            CommandService commands,
            Func<LogMessage, Task> logger = null)
            : base(commands, logger)
        {
        }
    }

    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    /// using the default  Guild, Channel, and User types. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    public class EFConfigStore<TContext> : EFConfigStore<TContext, ConfigUser>
        where TContext : EFBaseConfigContext
    {
        public EFConfigStore(
            CommandService commands,
            Func<LogMessage, Task> logger = null)
            : base(commands, logger)
        {
        }
    }
}

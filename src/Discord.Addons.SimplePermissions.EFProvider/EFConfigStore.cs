using System;
using System.Linq;
using System.Reflection;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <typeparam name="TGuild">The Guild configuration type.</typeparam>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class EFConfigStore<TContext, TGuild, TChannel, TUser> : IConfigStore<TContext>
        where TContext : EFBaseConfigContext<TGuild, TChannel, TUser>
        where TGuild : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser : ConfigUser, new()
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary> Initializes a new instance of <see cref="EFConfigStore{TContext}"/>. </summary>
        public EFConfigStore(CommandService commands,
            Action<DbContextOptionsBuilder> optionsaction)
        {
            _serviceProvider = new ServiceCollection()
                .AddSingleton(commands)
                .AddDbContext<TContext>(optionsaction)
                .BuildServiceProvider();

            using (var scope = _serviceProvider.CreateScope())
            {
                scope.ServiceProvider.GetService<TContext>().Database.Migrate();
            }
        }

        /// <summary> Loads an instance of the DB Context. </summary>
        public TContext Load()
        {
            return _serviceProvider.CreateScope().ServiceProvider.GetService<TContext>();
        }
    }

    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    /// using the default Guild type. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class EFConfigStore<TContext, TChannel, TUser> : EFConfigStore<TContext, ConfigGuild<TChannel, TUser>, TChannel, TUser>
        where TContext : EFBaseConfigContext<ConfigGuild<TChannel, TUser>, TChannel, TUser>
        where TChannel : ConfigChannel<TUser>, new()
        where TUser : ConfigUser, new()
    {
        public EFConfigStore(CommandService commands,
            Action<DbContextOptionsBuilder> optionsaction) : base(commands, optionsaction)
        {
        }
    }

    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    /// using the default Guild and Channel types. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public class EFConfigStore<TContext, TUser> : EFConfigStore<TContext, ConfigGuild<TUser>, ConfigChannel<TUser>, TUser>
        where TContext : EFBaseConfigContext<ConfigGuild<TUser>, ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser, new()
    {
        public EFConfigStore(CommandService commands,
            Action<DbContextOptionsBuilder> optionsaction) : base(commands, optionsaction)
        {
        }
    }

    /// <summary> Implementation of an <see cref="IConfigStore{TConfig}"/> using EF as a backing store,
    /// using the default  Guild, Channel, and User types. </summary>
    /// <typeparam name="TContext">The database context type.</typeparam>
    public class EFConfigStore<TContext> : EFConfigStore<TContext, ConfigGuild, ConfigChannel, ConfigUser>
        where TContext : EFBaseConfigContext<ConfigGuild, ConfigChannel, ConfigUser>
    {
        public EFConfigStore(CommandService commands,
            Action<DbContextOptionsBuilder> optionsaction) : base(commands, optionsaction)
        {
        }
    }
}

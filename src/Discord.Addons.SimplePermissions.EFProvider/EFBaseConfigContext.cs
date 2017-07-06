using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store. </summary>
    /// <typeparam name="TGuild">The Guild configuration type.</typeparam>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public abstract partial class EFBaseConfigContext<TGuild, TChannel, TUser> : DbContext
        where TGuild : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser : ConfigUser, new()
    {
        /// <summary> </summary>
        public DbSet<TGuild> Guilds { get; set; }

        /// <summary> </summary>
        public DbSet<TChannel> Channels { get; set; }

        /// <summary> </summary>
        public DbSet<TUser> Users { get; set; }

        /// <summary> </summary>
        public DbSet<ConfigModule> Modules { get; set; }

        /// <summary> </summary>
        protected virtual Task OnGuildAdd(TGuild guild)
        {
            guild.WhiteListedModules.Add(Modules.Single(m => m.ModuleName == PermissionsModule.PermModuleName));
            return Task.CompletedTask;
        }

        /// <summary> </summary>
        protected virtual Task OnChannelAdd(TChannel channel)
        {
            return Task.CompletedTask;
        }

        /// <summary> </summary>
        protected virtual Task OnUserAdd(TUser user)
        {
            return Task.CompletedTask;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ConfigModule>()
                .HasAlternateKey(e => e.ModuleName);

            modelBuilder.Entity<ConfigChannel<TUser>>()
                .HasAlternateKey(e => e.ChannelId);

            modelBuilder.Entity<ConfigGuild<TUser>>()
                .HasAlternateKey(e => e.GuildId);
        }
    }

    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store,
    /// using the default Guild type. </summary>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public abstract class EFBaseConfigContext<TChannel, TUser> : EFBaseConfigContext<ConfigGuild<TChannel, TUser>, TChannel, TUser>
        where TChannel : ConfigChannel<TUser>, new()
        where TUser : ConfigUser, new()
    {
    }

    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store,
    /// using the default Guild and Channel types. </summary>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public abstract class EFBaseConfigContext<TUser> : EFBaseConfigContext<ConfigGuild<TUser>, ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser, new()
    {
    }

    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store,
    /// using the default Guild, Channel and User types. </summary>
    public abstract class EFBaseConfigContext : EFBaseConfigContext<ConfigGuild, ConfigChannel, ConfigUser>
    {
    }
}

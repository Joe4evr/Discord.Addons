using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Discord;
using Discord.Commands;

#nullable disable warnings
namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    ///     Implementation of an <see cref="IPermissionConfig"/>
    ///     using an Entity Framework
    ///     <see cref="DbContext"/> as a backing store.
    /// </summary>
    /// <typeparam name="TGuild">
    ///     The Guild configuration type.
    /// </typeparam>
    /// <typeparam name="TChannel">
    ///     The Channel configuration type.
    /// </typeparam>
    /// <typeparam name="TUser">
    ///     The User configuration type.
    /// </typeparam>
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

        //Cross tables
        public DbSet<ChannelUser<TChannel, TUser>> ChannelUsers { get; set; }
        public DbSet<ChannelModule<TChannel, TUser>> ChannelModules { get; set; }
        public DbSet<GuildModule<TGuild, TChannel, TUser>> GuildModules { get; set; }

        internal IReadOnlyDictionary<string, ModuleInfo> ModuleInfos { get; set; }

        protected EFBaseConfigContext(DbContextOptions options)
            : base(options)
        {
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TGuild>(guild =>
            {
                guild.Property<long>("GuildSnowflake")
                    .HasField(nameof(ConfigGuild._gid))
                    .IsRequired(true);

                guild.HasIndex("GuildSnowflake")
                    .IsUnique(true);

                guild.Property<long>("AdminRoleSnowflake")
                    .HasField(nameof(ConfigGuild._aid))
                    .HasDefaultValue(0L)
                    .ValueGeneratedNever()
                    .IsRequired(true);

                guild.Property<long>("ModRoleSnowflake")
                    .HasField(nameof(ConfigGuild._mid))
                    .HasDefaultValue(0L)
                    .ValueGeneratedNever()
                    .IsRequired(true);

                guild.HasMany(g => g.WhiteListedModules);

                guild.HasMany(g => g.Channels);
            });

            modelBuilder.Entity<TChannel>(channel =>
            {
                channel.Property<long>("ChannelSnowflake")
                    .HasField(nameof(ConfigChannel._cid))
                    .IsRequired(true);

                channel.HasIndex("ChannelSnowflake")
                    .IsUnique(true);

                channel.HasMany(c => c.SpecialUsers);

                channel.HasMany(c => c.WhiteListedModules);
            });

            modelBuilder.Entity<TUser>(user =>
            {
                user.Property<long>("UserSnowflake")
                    .HasField(nameof(ConfigUser._uid))
                    .IsRequired(true);

                user.HasIndex("UserSnowflake")
                    .IsUnique(true);
            });

            modelBuilder.Entity<ConfigModule>(module =>
            {
                module.HasAlternateKey(e => e.ModuleName);
            });

            modelBuilder.Entity<ChannelUser<TChannel, TUser>>(channelUser =>
            {
                channelUser.HasOne(cu => cu.Channel);

                channelUser.HasOne(cu => cu.User);
            });

            modelBuilder.Entity<ChannelModule<TChannel, TUser>>(channelModule =>
            {
                channelModule.HasOne(cm => cm.Channel);

                channelModule.HasOne(cm => cm.Module);
            });

            modelBuilder.Entity<GuildModule<TGuild, TChannel, TUser>>(guildModule =>
            {
                guildModule.HasOne(gm => gm.Guild);

                guildModule.HasOne(gm => gm.Module);
            });

            modelBuilder.Ignore<ChannelModule<ConfigChannel<TUser>, TUser>>()
                .Ignore<ChannelUser<ConfigChannel<TUser>, TUser>>()
                .Ignore<GuildModule<ConfigGuild<TChannel, TUser>, TChannel, TUser>>();

            base.OnModelCreating(modelBuilder);
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

        public EFBaseConfigContext(DbContextOptions options)
            : base(options)
        {
        }
    }

    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store,
    /// using the default Guild and Channel types. </summary>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public abstract class EFBaseConfigContext<TUser> : EFBaseConfigContext<ConfigChannel<TUser>, TUser>
        where TUser : ConfigUser, new()
    {

        public EFBaseConfigContext(DbContextOptions options)
            : base(options)
        {
        }
    }

    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store,
    /// using the default Guild, Channel and User types. </summary>
    public abstract class EFBaseConfigContext : EFBaseConfigContext<ConfigUser>
    {

        public EFBaseConfigContext(DbContextOptions options)
            : base(options)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        /// <summary> </summary>
        public DbSet<ChannelUser<TChannel, TUser>> ChannelUsers { get; set; }
        /// <summary> </summary>
        public DbSet<ChannelModule<TChannel, TUser>> ChannelModules { get; set; }
        /// <summary> </summary>
        public DbSet<GuildModule<TGuild, TChannel, TUser>> GuildModules { get; set; }

        private IReadOnlyDictionary<string, ModuleInfo> ModuleInfos { get; }

        /// <summary> </summary>
        protected EFBaseConfigContext(DbContextOptions options)
            : base(options)
        {
            //var cmds = options.FindExtension<EFCommandServiceExtension>()?.CommandService;
            //ModuleInfos = (cmds?.Modules ?? Enumerable.Empty<ModuleInfo>()).ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
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

        /// <summary> </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var longUlongConverter = new ValueConverter<ulong, long>(
                ul => unchecked((long)ul),
                l => unchecked((ulong)l));


            modelBuilder.Entity<TGuild>(guild =>
            {
                guild.Property(g => g.GuildId)
                    .HasConversion(longUlongConverter)
                    .IsRequired(true);

                guild.HasIndex(g => g.GuildId)
                    .IsUnique(true);

                guild.Property(g => g.AdminRoleId)
                    .HasDefaultValue(0L)
                    .HasConversion(longUlongConverter)
                    .ValueGeneratedNever()
                    .IsRequired(true);

                guild.Property(g => g.ModRoleId)
                    .HasDefaultValue(0L)
                    .HasConversion(longUlongConverter)
                    .ValueGeneratedNever()
                    .IsRequired(true);

                guild.HasMany(g => g.WhiteListedModules);

                guild.HasMany(g => g.Channels);
            });

            modelBuilder.Entity<TChannel>(channel =>
            {
                channel.Property(c => c.ChannelId)
                    .HasConversion(longUlongConverter)
                    .IsRequired(true);

                channel.HasIndex(c => c.ChannelId)
                    .IsUnique(true);

                channel.HasMany(c => c.SpecialUsers);

                channel.HasMany(c => c.WhiteListedModules);
            });

            modelBuilder.Entity<TUser>(user =>
            {
                user.Property(u => u.UserId)
                    .HasConversion(longUlongConverter)
                    .IsRequired(true);

                user.HasIndex(u => u.UserId)
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
        /// <summary> </summary>
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
        /// <summary> </summary>
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
        /// <summary> </summary>
        public EFBaseConfigContext(DbContextOptions options)
            : base(options)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Discord;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Implementation of an <see cref="IPermissionConfig"/>
    /// using an Entity Framework
    /// <see cref="DbContext"/> as a backing store. </summary>
    /// <typeparam name="TGuild">The Guild configuration type.</typeparam>
    /// <typeparam name="TChannel">The Channel configuration type.</typeparam>
    /// <typeparam name="TUser">The User configuration type.</typeparam>
    public abstract partial class EFBaseConfigContext<TGuild, TChannel, TUser> : DbContext
        where TGuild   : ConfigGuild<TChannel, TUser>, new()
        where TChannel : ConfigChannel<TUser>, new()
        where TUser    : ConfigUser, new()
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
            modelBuilder.Entity<ChannelUser<TChannel, TUser>>()
                .HasOne(cu => cu.Channel);

            modelBuilder.Entity<ChannelUser<TChannel, TUser>>()
                .HasOne(cu => cu.User);

            modelBuilder.Entity<ChannelModule<TChannel, TUser>>()
                .HasOne(cm => cm.Channel);

            modelBuilder.Entity<ChannelModule<TChannel, TUser>>()
                .HasOne(cm => cm.Module);

            modelBuilder.Entity<GuildModule<TGuild, TChannel, TUser>>()
                .HasOne(gm => gm.Guild);

            modelBuilder.Entity<GuildModule<TGuild, TChannel, TUser>>()
                .HasOne(gm => gm.Module);



            modelBuilder.Entity<ConfigModule>()
                .HasAlternateKey(e => e.ModuleName);

            modelBuilder.Entity<TUser>()
                .Property<long>("User_Id")
                .HasField("_uid")
                .IsRequired(true);


            modelBuilder.Entity<TChannel>()
                .Property<long>("Channel_Id")
                .HasField("_cid")
                .IsRequired(true);


            modelBuilder.Entity<TChannel>()
                .HasMany(c => c.SpecialUsers);

            modelBuilder.Entity<TChannel>()
                .HasMany(c => c.WhiteListedModules);


            modelBuilder.Entity<TGuild>()
                .Property<long>("Guild_Id")
                .HasField("_gid")
                .IsRequired(true);

            modelBuilder.Entity<TGuild>()
                .Property<long>("Mod_Id")
                .HasField("_mid")
                .HasDefaultValue(0L)
                .ValueGeneratedNever()
                .IsRequired(true);

            modelBuilder.Entity<TGuild>()
                .Property<long>("Admin_Id")
                .HasField("_aid")
                .HasDefaultValue(0L)
                .ValueGeneratedNever()
                .IsRequired(true);


            modelBuilder.Entity<TGuild>()
                .HasMany(g => g.WhiteListedModules);

            modelBuilder.Entity<TGuild>()
                .HasMany(g => g.Channels);


            //modelBuilder.Entity<GuildModule<TGuild, TChannel, TUser>>()
            //    .HasAlternateKey(gm => new { gm.Guild, gm.Module });

            //modelBuilder.Entity<ChannelModule<TChannel, TUser>>()
            //    .HasAlternateKey(cm => new { cm.Channel, cm.Module });

            //modelBuilder.Entity<ChannelUser<TChannel, TUser>>()
            //    .HasAlternateKey(cu => new { cu.Channel, cu.User });

            // TODO: test this
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

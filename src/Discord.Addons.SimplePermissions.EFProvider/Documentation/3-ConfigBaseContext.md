The Config Base Context
----------

This class is the heart and soul of the provider.

```cs
public abstract class EFBaseConfigContext<TGuild, TChannel, TUser> : DbContext, IPermissionConfig
    where TGuild : ConfigGuild<TChannel, TUser>, new()
    where TChannel : ConfigChannel<TUser>, new()
    where TUser : ConfigUser, new()
{
    public DbSet<TGuild> Guilds { get; set; }

    public DbSet<TChannel> Channels { get; set; }

    public DbSet<TUser> Users { get; set; }

    public DbSet<ConfigModule> Modules { get; set; }

    protected Task OnGuildAdd(TGuild guild);

    protected Task OnChannelAdd(TChannel channel);

    protected Task OnUserAdd(TUser user);
}
```

To avoid generic parameter overload, `EFBaseConfigContext` and the `Config*` classes also
exist with different amounts of type parameters so it doesn't clutter up *your* code.

Now you have to inherit this class (or one with the appropriate type parameters)
and configure it to use the database of your choice.

**NOTE:** You should not put your connection strings directly in your code.
```cs
using Microsoft.EntityFrameworkCore;

public class ExampleConfigContext : EFBaseConfigContext<ExampleUser>
{
    // You can add more tables to your databse here by adding `DbSet<SomeType> Things { get; set; }`

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // For SQLite
        // A SQLite connection string looks like: "Filename=./DatabaseName.sqlite"
        optionsBuilder.UseSqlite(connectionString);
        
        // For SQL Server Compact
        // A SQL Server connection string looks like: "Server=(localdb)\mssqllocaldb;Database=DatabaseName;Trusted_Connection=True;"
        optionsBuilder.UseSqlServer(connectionString);

        // Remember that you should ABSOLUTELY call the base method
        base.OnConfiguring(optionsBuilder);
    }
}
```

[<- Part 2 - Object Tour](1-Intro.md) - The Config Base Context - [Part 4 - The Config Store ->](4-ConfigStore.md)
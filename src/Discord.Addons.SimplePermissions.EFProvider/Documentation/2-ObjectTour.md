Object Tour
-------------

There are four classes representing the data model,
three of which can bee freely inherited should you want to track
additional properties for your configuration.

```cs
public sealed class ConfigModule
{
    public int Id { get; set; }

    public string ModuleName { get; set; }
}

public class ConfigUser
{
    public int Id { get; set; }

    public ulong UserId { get; set; }

    public ulong GuildId { get; set; }
}

public class ConfigChannel<TUser>
    where TUser : ConfigUser
{
    public int Id { get; set; }

    public ulong ChannelId { get; set; }

    public ICollection<TUser> SpecialUsers { get; set; } = new List<TUser>();

    public ICollection<ConfigModule> WhiteListedModules { get; set; } = new List<ConfigModule>();
}

public class ConfigGuild<TChannel, TUser>
    where TChannel : ConfigChannel<TUser>
    where TUser : ConfigUser
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    public ulong ModRole { get; set; }

    public ulong AdminRole { get; set; }

    public ICollection<TChannel> Channels { get; set; } = new List<TChannel>();

    public ICollection<TUser> Users { get; set; } = new List<TUser>();

    public ICollection<ConfigModule> WhiteListedModules { get; set; } = new List<ConfigModule>();
}
```

Aside from `ConfigModule`, you are free to inherit these classes and add more properties.
For example, you might want to have users earn points of some kind over time.

```cs
public class ExampleUser : ConfigUser
{
    public int Points { get; set; }
}
```

[<- Part 1 - Intro](1-Intro.md) - Object Tour - [Part 3 - The Config Base Context ->](3-ConfigBaseContext.md)
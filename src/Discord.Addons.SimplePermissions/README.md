## Discord.Addons.SimplePermissions
Discord.Net module for command permission management.

### Quickstart
You need a config store provider for this. I have basic
implementations for both JSON, and databases via EF Core available.

First you need a config object; you can use a base
implementation if using a pre-made provider.
```cs
//example for the JSON provider
public class MyBotConfig : JsonConfigBase
{
    // You could add some more properties here that don't exist in the base class
    // for example:
    public string Token { get; set; }
}
```

Next, create an instance of your config store as your bot starts
up and call the `UseSimplePermissions()` extension method.
```cs
// At the top in your Program class:
private readonly IConfigStore<MyBotConfig> configstore = new JsonConfigStore<MyBotConfig>("config.json");

// When adding all your modules:
await commandService.UseSimplePermissions(client, configstore, map, Logger);
// 'Logger' is an optional delegate that can point to your logging method
```

Now you can also use the `configstore` for other parts of your configuration.
Note, since the config now has to be an `IDisposable` too, wrap all the access
to the config object in a `using` statement:
```cs
using (var config = configstore.Load())
{
    await client.LoginAsync(TokenType.Bot, config.Token);
}
```

### Permissions
For optimal management, you should mark every command
you make with the Permission attribute to specify who
can use which commands. For example:
```cs
// When using method commands:
[Permission(MinimumPermission.ModRole)]
public async Task MyCmd()
{
    //.....
}

// When using Command builders:
//....
.AddPrecondition(new PermissionAttribute(MinimumPermission.ModRole))
//....
```

There are six permission levels provided:
```cs
Everyone = 0,
ModRole = 1,
AdminRole = 2,
GuildOwner = 3,
Special,
BotOwner
```

The first four levels are in a hierarchy; these can execute
commands of their own level or below. `Special` and `BotOwner` are **not**
part of the permission hierarchy, these must match ***exactly*** in order to pass.

Once your bot has joined a new Guild (server), the owner of that Guild has to set
which roles are considered Mods and Admins respectively.
The `roles` command can be used to list all roles and their ID,
then `setmod <id>` and `setadmin <id>` to do it.

Once the roles are set, Mods and above can whitelist/blacklist specific Modules either
per Channel or per Guild. The `modules` command will list the names of all
Modules registered in the CommandService, then use `wl <module name>`
to whitelist a Module in that Channel, or `wl <module name> g` to use it in all channels.

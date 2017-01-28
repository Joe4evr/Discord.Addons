## Discord.Addons.SimplePermissions
Discord.Net module for command permission management.

### Quickstart
You need a config store provider for this. I have basic implementations for both JSON, and databases via EF Core available.

First you need a config object; you can use a base implementation if using a pre-made provider.
```cs
//example for the JSON provider
public class MyBotConfig : JsonConfigBase
{
    //you could add some more properties here that don't exist in the base class
	//for example:
	public string Token { get; set; }
}
```

Next, create an instance of your config store as your bot starts up and call the `AddPermissionsService()` extension method.
```cs
var configstore = new JsonConfigStore<MyBotConfig>("config.json");

await commandService.AddPermissionsService(client, configstore, map);
```

Now you can also use the `configstore` for other parts of your configuration.
```cs
await client.LoginAsync(TokenType.Bot, config.Load().Token);
```
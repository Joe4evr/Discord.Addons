using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using Discord.WebSocket;

namespace Examples.SimplePermissions.JsonProvider
{
    class Program
    {
        // Program entry point
        static void Main(string[] args)
        {
            // Standard 1.0 fare
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        // Keep the config store in a private field, just like the client.
        private readonly IConfigStore<MyJsonConfig> _configStore;

        // Standard 1.0 fare
        private readonly DiscordSocketClient _client;
        private readonly IServiceCollection _map = new ServiceCollection();
        private readonly CommandService _commands = new CommandService();

        private static Task Log(LogMessage message)
        {
            // Your preferred logging implementation here
            return Task.CompletedTask;
        }

        private Program()
        {
            // One neat thing you could do is pass 'args[0]' from Main()
            // into the constructor here and use that as the path so that
            // you can specify where the config is loaded from when the bot starts up.
            _configStore = new JsonConfigStore<MyJsonConfig>("path_to_config.json", _commands);

            // If you have some Dictionary of data added to your config,
            // you should add a check to see if is initialized already or not.
            using (var config = _configStore.Load())
            {
                if (config.SomeData == null)
                {
                    config.SomeData = new Dictionary<ulong, string[]>();
                    // Remember to call Save() to save any changes
                    config.Save();
                }
            }

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                // Standard 1.0 fare
            });
        }

        private async Task MainAsync()
        {
            // More standard 1.0 fare here...

            // NOTE: Because SimplePermissions dictates a lot of behavior,
            // it will not get auto-loaded with `AddModulesAsync(Assembly.GetEntryAssembly())`.
            // You have to use the initializer method to explicitly add it to the CommandService.

            // You can pass your Logging method into the initializer for
            // SimplePermissions, so that you get a consistent looking log:
            await _commands.UseSimplePermissions(_client, _configStore, _map, Log);

            // Load the config, read the token, and pass it into the login method:
            using (var config = _configStore.Load())
            {
                await _client.LoginAsync(TokenType.Bot, config.LoginToken);
            }

            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }
    }
}

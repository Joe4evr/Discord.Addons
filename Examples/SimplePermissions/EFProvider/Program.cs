using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Addons.SimplePermissions;
using Discord.Commands;
using Discord.WebSocket;

namespace Examples.SimplePermissions.EFProvider
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
        private readonly IConfigStore<MyEFConfig> _configStore;

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
            _commands.Log += Log;

            // Initialize the config store. You could pass the process arguments
            // down to the Program constructor to read the connection string from there.
            // You can also choose a different database provider instead of SQLite
            // as long as it is compatible with EF Core.
            _configStore = new EFConfigStore<MyEFConfig, MyConfigGuild, MyConfigChannel, MyConfigUser>(_commands,
                options => options.UseSqlite("connection_string_here"));

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

            await _client.LoginAsync(TokenType.Bot, "login_token_here");
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }
    }
}

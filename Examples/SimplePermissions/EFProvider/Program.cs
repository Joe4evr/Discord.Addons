//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Discord;
//using Discord.Addons.SimplePermissions;
//using Discord.Commands;
//using Discord.WebSocket;

//namespace Examples.SimplePermissions.EFProvider
//{
//    class Program
//    {
//        // Program entry point
//        static async Task Main(string[] args)
//        {
//            // Standard 1.0 fare
//            await new Program().MainAsync();
//        }

//        // Keep the config store in a private field, just like the client.
//        private readonly IConfigStore<MyEFConfig> _configStore;

//        // Standard 1.0 fare
//        private readonly DiscordSocketClient _client;
//        private readonly IServiceProvider _services;
//        private readonly CommandService _commands = new CommandService();

//        private static Task Log(LogMessage message)
//        {
//            // Your preferred logging implementation here
//            return Task.CompletedTask;
//        }

//        private Program()
//        {
//            _commands.Log += Log;

//            // Initialize the config store. You could pass the process arguments
//            // down to the Program constructor to read the connection string from there.
//            // You can also choose a different database provider instead of SQLite
//            // as long as it is compatible with EF Core.
//            _configStore = new EFConfigStore<MyEFConfig, MyConfigGuild, MyConfigChannel, MyConfigUser>(_commands, Log);

//            _client = new DiscordSocketClient(new DiscordSocketConfig
//            {
//                // Standard 1.0 fare
//            });

//            _services = ConfigureServices(_client, _commands, _configStore);
//        }

//        private static IServiceProvider ConfigureServices(
//            DiscordSocketClient client,
//            CommandService commands,
//            IConfigStore<MyEFConfig> configStore)
//        {
//            // You can pass your Logging method into the initializer for
//            // SimplePermissions, so that you get a consistent looking log:
//            var map = new ServiceCollection()
//                .AddSingleton(new PermissionsService(configStore, commands, client, Log))
//                .AddDbContext<MyEFConfig>(options => options.UseSqlite("connection_string_here"));

//            return map.BuildServiceProvider();
//        }

//        private async Task MainAsync()
//        {
//            // More standard 1.0 fare here...

//            await _commands.AddModuleAsync<PermissionsModule>(_services);

//            using (var config = _configStore.Load())
//            {
//                await _client.LoginAsync(TokenType.Bot, config.GetLoginToken());
//            }
//            await _client.StartAsync();
//            await Task.Delay(Timeout.Infinite);
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace Examples.MpGame
{
    class Program
    {
        static async Task Main()
        {
            var p = new Program();
            await p.InitCommands();
        }

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        private Program()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
            });

            _commands = new CommandService(new CommandServiceConfig
            {
            });

            _services = ConfigureServices(_client);
        }

        private static IServiceProvider ConfigureServices(DiscordSocketClient client)
        {
            var map = new ServiceCollection()
                .AddSingleton(new ExampleGameService(client));

            return map.BuildServiceProvider();
        }

        private async Task InitCommands()
        {
            await _commands.AddModuleAsync<ExampleGameModule>(_services);
        }
    }
}

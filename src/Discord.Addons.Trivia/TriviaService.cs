using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace Discord.Addons.TriviaGames
{
    /// <summary> Manages Trivia games. </summary>
    public sealed class TriviaService
    {
        internal readonly IReadOnlyDictionary<string, string[]> TriviaData;
        internal IReadOnlyDictionary<ulong, TriviaGame> TriviaGames => _triviaGames;

        private readonly ConcurrentDictionary<ulong, TriviaGame> _triviaGames =
            new ConcurrentDictionary<ulong, TriviaGame>();

        /// <summary> Create the service that will manage Trivia games. </summary>
        /// <param name="triviaData">A set of questions and answers to use as trivia.</param>
        /// <param name="client">The <see cref="DiscordSocketClient"/> instance.</param>
        public TriviaService(IReadOnlyDictionary<string, string[]> triviaData, DiscordSocketClient client)
        {
            TriviaData = triviaData ?? throw new ArgumentNullException(nameof(triviaData));

            client.MessageReceived += msg =>
            {
                return (_triviaGames.TryGetValue(msg.Channel.Id, out var game))
                    ? game.CheckTrivia(msg)
                    : Task.CompletedTask;
            };

            Console.WriteLine($"{DateTime.Now,20}: Created Trivia service.");
        }

        /// <summary> Add a new game to the list of active games. </summary>
        /// <param name="channelId">Public facing channel of this game.</param>
        /// <param name="game">Instance of the game.</param>
        public void AddNewGame(ulong channelId, TriviaGame game)
        {
            if (_triviaGames.TryAdd(channelId, game))
                game.GameEnd += _onGameEnd;
        }

        private Task _onGameEnd(ulong channelId)
        {
            if (_triviaGames.TryRemove(channelId, out var game))
            {
                game.GameEnd -= _onGameEnd;
            }
            return Task.CompletedTask;
        }
    }

    public static class TriviaExtensions
    {
        public static Task AddTrivia<TTrivia>(
            this CommandService cmdService,
            IDependencyMap map,
            IReadOnlyDictionary<string, string[]> triviaData,
            DiscordSocketClient client,
            TTrivia triviaModule)
            where TTrivia : TriviaModuleBase
        {
            map.Add(new TriviaService(triviaData, client));
            return cmdService.AddModuleAsync<TTrivia>();
        }
    }
}

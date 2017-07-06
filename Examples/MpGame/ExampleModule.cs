﻿using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.MpGame;
using Discord.Commands;

namespace Examples.MpGame
{
    public sealed class ExampleModule : MpGameModuleBase< // Inherit MpGameModuleBase
        ExampleService, // Specify the type of the service that will keep track of running games
        ExampleGame, Player> // Specify the type of the game and the type of its player
    {
        public ExampleModule(ExampleService gameService)
            : base(gameService)
        {
        }

        //Do stuff to get external data if needed
        protected override void BeforeExecute(CommandInfo command)
        {
            base.BeforeExecute(command);
            GameService.DataDictionary.TryGetValue(Context.Channel, out _data);
        }

        private DataType _data;

        // You may have reasons to not annotate a particular method with [Command],
        // and you'll likely have to add MORE commands depending on the game
        [Command("opengame")]
        public override async Task OpenGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
            }
            else if (OpenToJoin)
            {
                await ReplyAsync("There is already a game open to join.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.OpenNewGame(Context.Channel))
                {
                    await ReplyAsync("Opening for a game.").ConfigureAwait(false);
                }
            }
        }

        // Note that we should always check if the channel already has a game going
        // or wants people to join before taking action
        [Command("join")]
        public override async Task JoinGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot join a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to join.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.AddUser(Context.Channel, Context.User))
                {
                    await ReplyAsync($"**{Context.User.Username}** has joined.").ConfigureAwait(false);
                }
            }
        }

        [Command("leave")] // Users can leave if the game hasn't started yet
        public override async Task LeaveGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot leave a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to leave.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.RemoveUser(Context.Channel, Context.User))
                {
                    await ReplyAsync($"**{Context.User.Username}** has left.").ConfigureAwait(false);
                }
            }
        }

        [Command("cancel")] // Cancel the game if it hasn't started yet
        public override async Task CancelGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot cancel a game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to cancel.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.CancelGame(Context.Channel))
                {
                    await ReplyAsync("Game was canceled.").ConfigureAwait(false);
                }
            }
        }

        [Command("start")] // Start the game
        public override async Task StartGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.").ConfigureAwait(false);
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game has been opened at this time.").ConfigureAwait(false);
            }
            else if (PlayerList.Count < 4) // Example value if a game has a minimum player requirement
            {
                await ReplyAsync("Not enough players have joined.").ConfigureAwait(false);
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel, newValue: false, comparisonValue: true))
                {
                    // Tip: Shuffle the players before projecting them
                    var players = PlayerList.Select(u => new Player(u, Context.Channel));
                    // The Player class can also be extended for additional properties

                    var game = new ExampleGame(Context.Channel, players);
                    if (GameService.TryAddNewGame(Context.Channel, game))
                    {
                        await game.SetupGame().ConfigureAwait(false);
                        await game.StartGame().ConfigureAwait(false);
                    }
                }
            }
        }

        [Command("turn")] // Advance to the next turn
        public override Task NextTurnCmd()
            => GameInProgress ? Game.NextTurn() : ReplyAsync("No game in progress.");

        // Post a message that represents the game's state
        [Command("state")] //Remember there's a 2000 character limit
        public override Task GameStateCmd()
           => GameInProgress ? ReplyAsync(Game.GetGameState()) : ReplyAsync("No game in progress.");

        // Command to end a game before a win-condition is met
        [Command("end")] //Should be restricted to mods/admins to prevent abuse
        public override Task EndGameCmd()
            => !GameInProgress ? ReplyAsync("No game in progress to end.") : Game.EndGame("Game ended early by moderator.");
    }
}

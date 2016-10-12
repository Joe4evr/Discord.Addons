using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.Commands;

namespace Example
{
    public sealed class ExampleModule : MpGameModuleBase<ExampleGame, Player> //Specify the type of the game and the type of its player
    {
        public ExampleModule(MpGameService<ExampleGame, Player> gameService)
            : base(gameService)
        {
        }

        //You may have reasons to not annotate a particular method with [Command],
        [Command("opengame")] //and you'll likely have to add MORE commands depending on the game
        public override async Task OpenGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.");
            }
            else if (OpenToJoin)
            {
                await ReplyAsync("There is already a game open to join.");
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: true, comparisonValue: false))
                {
                    //UserComparer is a property on the base class you can use to determine user-uniqueness
                    GameService.SetPlayerList(Context.Channel.Id, new HashSet<IGuildUser>(UserComparer));
                    await ReplyAsync("Opening for a game.");
                }
            }
        }

        //Note that we should always check if the channel already has a game going
        [Command("join")] //or wants people to join before taking action
        public override async Task JoinGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot join a game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to join.");
            }
            else
            {
                var author = Context.User as IGuildUser;
                if (author != null && PlayerList.Add(author))
                {
                    await ReplyAsync($"**{author.Username}** has joined.");
                }
            }
        }

        [Command("leave")] //Users can leave if the game hasn't started yet
        public override async Task LeaveGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot leave a game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to leave.");
            }
            else
            {
                var author = Context.User as IGuildUser;
                if (author != null && PlayerList.Remove(author))
                {
                    await ReplyAsync($"**{author.Username}** has left.");
                }
            }
        }

        [Command("cancel")] //Cancel the game if it hasn't started yet
        public override async Task CancelGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Cannot cancel a game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game open to cancel.");
            }
            else
            {
                if (GameService.TryUpdateOpenToJoin(Context.Channel.Id, newValue: false, comparisonValue: true))
                {
                    PlayerList.Clear();
                    await ReplyAsync("Game was canceled.");
                }
            }
        }

        [Command("start")] //Start the game
        public override async Task StartGameCmd()
        {
            if (GameInProgress)
            {
                await ReplyAsync("Another game already in progress.");
            }
            else if (!OpenToJoin)
            {
                await ReplyAsync("No game has been opened at this time.");
            }
            else if (PlayerList.Count < 4) //Example value if a game has a minimum player requirement
            {
                await ReplyAsync("Not enough players have joined.");
            }
            else
            {
                //Tip: Shuffle the players before projecting them
                var players = PlayerList.Select(u => new Player(u, Context.Channel));
                //The Player class can also be extended for additional properties

                var game = new ExampleGame(Context.Channel, players);
                GameService.SetOpenToJoin(Context.Channel.Id, false);
                GameService.AddNewGame(Context.Channel.Id, game);
                GameService.SetInProgress(Context.Channel.Id, true);
                await game.SetupGame();
                await game.StartGame();
            }
        }

        [Command("turn")] //Advance to the next turn
        public override Task NextTurnCmd()
            => Game != null ? Game.NextTurn() : ReplyAsync("No game in progress.");

        //Post a message that represents the game's state
        [Command("state")] //Remember there's a 2000 character limit
        public override Task GameStateCmd()
           => Game != null ? ReplyAsync(Game.GetGameState()) : ReplyAsync("No game in progress.");

        //Command to end a game before a win-condition is met
        [Command("end")] //Should be restricted to mods/admins to prevent abuse
        public override async Task EndGameCmd()
        {
            ExampleGame game;
            if (!GameInProgress)
            {
                await ReplyAsync("No game in progress to end.");
            }
            else if (GameService.TryRemoveGame(Context.Channel.Id, out game))
            {
                await game.EndGame("Game ended early by moderator.");
                GameService.SetInProgress(Context.Channel.Id, false);
            }
        }
    }
}

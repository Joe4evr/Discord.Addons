using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.MpGame;
using Discord.WebSocket;
using Discord;

namespace Example
{
    [Module("ex-")] //Needed to load methods as commands
    public sealed class ExampleModule : MpGameModuleBase<ExampleGame, Player> //Specify the type of the game and the type of its player
    {
        public ExampleModule(DiscordSocketClient client) : base(client)
        {
        }

                              //You may have reasons to not annotate a particular method with [Command],
        [Command("opengame")] //and you'll likely have to add MORE commands depending on the game
        public override async Task OpenGameCmd(IMessage msg)
        {
            bool gip;
            bool open;
            if (gameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Another game already in progress.");
            }
            else if (openToJoin.TryGetValue(msg.Channel.Id, out open) && open)
            {
                await msg.Channel.SendMessageAsync("There is already a game open to join.");
            }
            else
            {
                if (openToJoin.TryUpdate(msg.Channel.Id, newValue: true, comparisonValue: false))
                {
                    playerList[msg.Channel.Id] = new ConcurrentDictionary<ulong, IGuildUser>();
                    await msg.Channel.SendMessageAsync("Opening for a game.");
                }
            }
        }

                          //Note that we should always check if the channel already has a game going
        [Command("join")] //or wants people to join before taking action
        public override async Task JoinGameCmd(IMessage msg)
        {
            bool gip;
            bool open;
            if (gameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Cannot join a game already in progress.");
            }
            else if (!openToJoin.TryGetValue(msg.Channel.Id, out open) || !open)
            {
                await msg.Channel.SendMessageAsync("No game open to join.");
            }
            else
            {
                var author = msg.Author as IGuildUser;
                if (author != null)
                {
                    if (playerList[msg.Channel.Id].TryAdd(author.Id, author))
                        await msg.Channel.SendMessageAsync($"**{author.Username}** has joined.");
                }
            }
        }

        [Command("leave")] //Users can leave if the game hasn't started yet
        public override async Task LeaveGameCmd(IMessage msg)
        {
            bool gip;
            bool open;
            if (gameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Cannot leave a game already in progress.");
            }
            else if (!openToJoin.TryGetValue(msg.Channel.Id, out open) || !open)
            {
                await msg.Channel.SendMessageAsync("No game open to leave.");
            }
            else
            {
                var author = msg.Author as IGuildUser;
                if (author != null && playerList[msg.Channel.Id].TryRemove(author.Id, out author))
                {
                    await msg.Channel.SendMessageAsync($"**{author.Username}** has left.");
                }
            }
        }

        [Command("cancel")] //Cancel the game if it hasn't started yet
        public override async Task CancelGameCmd(IMessage msg)
        {
            bool gip;
            bool open;
            if (gameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Cannot cancel a game already in progress.");
            }
            else if (!openToJoin.TryGetValue(msg.Channel.Id, out open) || !open)
            {
                await msg.Channel.SendMessageAsync("No game open to cancel.");
            }
            else
            {
                if (openToJoin.TryUpdate(msg.Channel.Id, newValue: false, comparisonValue: true))
                { 
                    playerList[msg.Channel.Id].Clear();
                    await msg.Channel.SendMessageAsync("Game was cancelled.");
                }
            }
        }

        [Command("start")] //Start the game
        public override async Task StartGameCmd(IMessage msg)
        {
            bool gip;
            bool open;
            if (gameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Another game already in progress.");
            }
            else if (!openToJoin.TryGetValue(msg.Channel.Id, out open) || !open)
            {
                await msg.Channel.SendMessageAsync("No game has been opened at this time.");
            }
            else if (playerList[msg.Channel.Id].Count < 4) //Example value if a game has a minimum player requirement
            {
                await msg.Channel.SendMessageAsync("Not enough players have joined.");
            }
            else
            {
                //Tip: Shuffle the players before selecting them
                var players = playerList[msg.Channel.Id].Select(u => new Player(u.Value));
                //The Player class can also be extended for additional properties

                openToJoin[msg.Channel.Id] = false;
                gameList[msg.Channel.Id] = new ExampleGame(msg.Channel, players);
                gameInProgress[msg.Channel.Id] = true;
                await gameList[msg.Channel.Id].SetupGame();
                await gameList[msg.Channel.Id].StartGame();
            }
        }

        [Command("turn")] //Advance to the next turn
        public override async Task NextTurnCmd(IMessage msg)
        {
            ExampleGame game;
            if (gameList.TryGetValue(msg.Channel.Id, out game))
            {
                await game.NextTurn();
            }
            else
            {
                await msg.Channel.SendMessageAsync("No game in progress.");
            }
        }

                           //Post a message that represents the game's state
        [Command("state")] //Remember there's a 2000 character limit
        public override async Task GameStateCmd(IMessage msg)
        {
            ExampleGame game;
            if (gameList.TryGetValue(msg.Channel.Id, out game))
            {
                await msg.Channel.SendMessageAsync(game.GetGameState());
            }
            else
            {
                await msg.Channel.SendMessageAsync("No game in progress.");
            }
        }

                         //Command to end a game before a win-condition is met
        [Command("end")] //Should be restricted to mods/admins to prevent abuse
        public override async Task EndGameCmd(IMessage msg)
        {
            bool gip;
            if (!gameInProgress.TryGetValue(msg.Channel.Id, out gip) || !gip)
            {
                await msg.Channel.SendMessageAsync("No game in progress to end.");
            }
            else
            {
                ExampleGame game;
                if (gameInProgress.TryUpdate(msg.Channel.Id, newValue: false, comparisonValue: true) &&
                    gameList.TryRemove(msg.Channel.Id, out game))
                {
                    await game.EndGame("Game ended early by moderator.");
                }
            }
        }
    }
}

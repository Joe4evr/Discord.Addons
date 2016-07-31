using System;
using System.Linq;
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
            if (gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("Another game already in progress.");
            }
            else if (openToJoin[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("There is already a game open to join.");
            }
            else
            {
                openToJoin[msg.Channel.Id] = true;
                await msg.Channel.SendMessageAsync("Opening for a game.");
            }
        }

                          //Note that we should always check if the channel already has a game going
        [Command("join")] //or wants people to join before taking action
        public override async Task JoinGameCmd(IMessage msg)
        {
            if (gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("Cannot join a game already in progress.");
            }
            else if (!openToJoin[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("No game open to join.");
            }
            else
            {
                var author = msg.Author as IGuildUser;
                if (author != null)
                {
                    playerList[msg.Channel.Id].Add(author);
                    await msg.Channel.SendMessageAsync($"**{author.Username}** has joined.");
                }
            }
        }

        [Command("leave")] //Users can leave if the game hasn't started yet
        public override async Task LeaveGameCmd(IMessage msg)
        {
            if (gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("Cannot leave a game already in progress.");
            }
            else if (!openToJoin[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("No game open to leave.");
            }
            else
            {
                var author = msg.Author as IGuildUser;
                if (author != null && playerList[msg.Channel.Id].Any(u => u.Id == author.Id))
                {
                    playerList[msg.Channel.Id].Remove(author);
                    await msg.Channel.SendMessageAsync($"**{author.Username}** has left.");
                }
            }
        }

        [Command("cancel")] //Cancel the game if it hasn't started yet
        public override async Task CancelGameCmd(IMessage msg)
        {
            if (gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("Cannot cancel a game already in progress.");
            }
            else if (!openToJoin[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("No game open to cancel.");
            }
            else
            {
                openToJoin[msg.Channel.Id] = false;
                playerList[msg.Channel.Id].Clear();
                await msg.Channel.SendMessageAsync("Game was cancelled.");
            }
        }

        [Command("start")] //Start the game
        public override async Task StartGameCmd(IMessage msg)
        {
            if (gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("Another game already in progress.");
            }
            else if (!openToJoin[msg.Channel.Id])
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
                var players = playerList[msg.Channel.Id].Select(u => new Player(u));
                //The Player class can also be extended for additional properties

                openToJoin[msg.Channel.Id] = false;
                gameList[msg.Channel.Id] = new ExampleGame(msg.Channel, players, client);
                gameInProgress[msg.Channel.Id] = true;
                await gameList[msg.Channel.Id].SetupGame();
                await gameList[msg.Channel.Id].StartGame();
            }
        }

        [Command("turn")] //Advance to the next turn
        public override async Task NextTurnCmd(IMessage msg)
        {
            if (gameInProgress[msg.Channel.Id])
            {
                await gameList[msg.Channel.Id].NextTurn();
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
            if (gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync(gameList[msg.Channel.Id].GetGameState());
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
            if (!gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("No game in progress to end.");
            }
            else
            {
                await gameList[msg.Channel.Id].EndGame("Game ended early by moderator.");
                gameInProgress[msg.Channel.Id] = false;
            }
        }
    }
}

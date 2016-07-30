using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Addons.MpGame;
using Discord.WebSocket;
using Discord;

namespace Example
{
    [Module]
    public sealed class ExampleModule : MpGameModuleBase<ExampleGame>
    {
        public ExampleModule(DiscordSocketClient client) : base(client)
        {
        }

        [Command("opengame")]
        public override async Task OpenGame(IMessage msg)
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

        [Command("join")]
        public override async Task JoinGame(IMessage msg)
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

        [Command("leave")]
        public override async Task LeaveGame(IMessage msg)
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
                    playerList[msg.Channel.Id].Add(author);
                    await msg.Channel.SendMessageAsync($"**{author.Username}** has left.");
                }
            }
        }

        [Command("cancel")]
        public override async Task CancelGame(IMessage msg)
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

        [Command("start")]
        public override async Task StartGame(IMessage msg)
        {
            if (gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("Another game already in progress.");
            }
            else if (!openToJoin[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("No game has been opened at this time.");
            }
            else if (playerList[msg.Channel.Id].Count < 4) //example value
            {
                await msg.Channel.SendMessageAsync("Not enough players have joined.");
            }
            else
            {
                openToJoin[msg.Channel.Id] = false;
                gameList[msg.Channel.Id] = new ExampleGame(msg.Channel, playerList[msg.Channel.Id].Select(u => new Player(u)), client);
                gameInProgress[msg.Channel.Id] = true;
                await gameList[msg.Channel.Id].SetupGame();
                await gameList[msg.Channel.Id].StartGame();
            }
        }

        [Command("turn")]
        public override async Task NextTurn(IMessage msg)
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

        [Command("state")]
        public override async Task GameState(IMessage msg)
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

        [Command("end")]
        public override async Task EndGame(IMessage msg)
        {
            if (!gameInProgress[msg.Channel.Id])
            {
                await msg.Channel.SendMessageAsync("");
            }
            else
            {
                gameInProgress[msg.Channel.Id] = false;
                await gameList[msg.Channel.Id].EndGame("Game ended early by moderator.");
            }
        }
    }
}

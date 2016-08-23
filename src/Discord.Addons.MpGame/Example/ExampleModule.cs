using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.Commands;

namespace Example
{
    [Module("ex-")] //Needed to load methods as commands
    public sealed class ExampleModule : MpGameModuleBase<ExampleGame, Player> //Specify the type of the game and the type of its player
    {
                              //You may have reasons to not annotate a particular method with [Command],
        [Command("opengame")] //and you'll likely have to add MORE commands depending on the game
        public override async Task OpenGameCmd(IMessage msg)
        {
            bool gip;
            bool open;
            if (GameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Another game already in progress.");
            }
            else if (OpenToJoin.TryGetValue(msg.Channel.Id, out open) && open)
            {
                await msg.Channel.SendMessageAsync("There is already a game open to join.");
            }
            else
            {
                if (OpenToJoin.TryUpdate(msg.Channel.Id, newValue: true, comparisonValue: false))
                {
                    PlayerList[msg.Channel.Id] = new HashSet<IGuildUser>(UserComparer);
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
            if (GameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Cannot join a game already in progress.");
            }
            else if (!OpenToJoin.TryGetValue(msg.Channel.Id, out open) || !open)
            {
                await msg.Channel.SendMessageAsync("No game open to join.");
            }
            else
            {
                var author = msg.Author as IGuildUser;
                if (author != null)
                {
                    if (PlayerList[msg.Channel.Id].Add(author))
                        await msg.Channel.SendMessageAsync($"**{author.Username}** has joined.");
                }
            }
        }

        [Command("leave")] //Users can leave if the game hasn't started yet
        public override async Task LeaveGameCmd(IMessage msg)
        {
            bool gip;
            bool open;
            if (GameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Cannot leave a game already in progress.");
            }
            else if (!OpenToJoin.TryGetValue(msg.Channel.Id, out open) || !open)
            {
                await msg.Channel.SendMessageAsync("No game open to leave.");
            }
            else
            {
                var author = msg.Author as IGuildUser;
                if (author != null && PlayerList[msg.Channel.Id].Remove(author))
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
            if (GameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Cannot cancel a game already in progress.");
            }
            else if (!OpenToJoin.TryGetValue(msg.Channel.Id, out open) || !open)
            {
                await msg.Channel.SendMessageAsync("No game open to cancel.");
            }
            else
            {
                if (OpenToJoin.TryUpdate(msg.Channel.Id, newValue: false, comparisonValue: true))
                { 
                    PlayerList[msg.Channel.Id].Clear();
                    await msg.Channel.SendMessageAsync("Game was cancelled.");
                }
            }
        }

        [Command("start")] //Start the game
        public override async Task StartGameCmd(IMessage msg)
        {
            bool gip;
            bool open;
            if (GameInProgress.TryGetValue(msg.Channel.Id, out gip) && gip)
            {
                await msg.Channel.SendMessageAsync("Another game already in progress.");
            }
            else if (!OpenToJoin.TryGetValue(msg.Channel.Id, out open) || !open)
            {
                await msg.Channel.SendMessageAsync("No game has been opened at this time.");
            }
            else if (PlayerList[msg.Channel.Id].Count < 4) //Example value if a game has a minimum player requirement
            {
                await msg.Channel.SendMessageAsync("Not enough players have joined.");
            }
            else
            {
                //Tip: Shuffle the players before selecting them
                var players = PlayerList[msg.Channel.Id].Select(u => new Player(u));
                //The Player class can also be extended for additional properties

                OpenToJoin[msg.Channel.Id] = false;
                GameList[msg.Channel.Id] = new ExampleGame(msg.Channel, players);
                GameInProgress[msg.Channel.Id] = true;
                await GameList[msg.Channel.Id].SetupGame();
                await GameList[msg.Channel.Id].StartGame();
            }
        }

        [Command("turn")] //Advance to the next turn
        public override async Task NextTurnCmd(IMessage msg)
        {
            ExampleGame game;
            if (GameList.TryGetValue(msg.Channel.Id, out game))
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
            if (GameList.TryGetValue(msg.Channel.Id, out game))
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
            if (!GameInProgress.TryGetValue(msg.Channel.Id, out gip) || !gip)
            {
                await msg.Channel.SendMessageAsync("No game in progress to end.");
            }
            else
            {
                ExampleGame game;
                if (GameInProgress.TryUpdate(msg.Channel.Id, newValue: false, comparisonValue: true) &&
                    GameList.TryRemove(msg.Channel.Id, out game))
                {
                    await game.EndGame("Game ended early by moderator.");
                }
            }
        }
    }
}

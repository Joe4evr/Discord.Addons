using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.MpGame;
using Discord.WebSocket;

namespace Example
{
    public sealed class ExampleGame : GameBase
    {
        private int _turn = 0;
        private GameState _state = GameState.Setup;

        public ExampleGame(IMessageChannel channel, IEnumerable<Player> players, DiscordSocketClient client)
            : base(channel, players, client)
        {
        }

        protected override async Task OnDmMessage(IMessage msg)
        {
            if (msg.Author.Id == TurnPlayer.Id && _state == GameState.MainPhase)
            {
                await msg.Channel.SendMessageAsync("PM received.");
            }
        }

        protected override async Task OnPublicMessage(IMessage msg)
        {
            if (msg.Author.Id == TurnPlayer.Id && _state == GameState.SpecialPhase)
            {
                await Channel.SendMessageAsync("Message acknowledged.");
            }
        }

        public override async Task SetupGame()
        {
            await Channel.SendMessageAsync("Asserting randomized starting parameters.");
        }

        public override async Task StartGame()
        {
            await Channel.SendMessageAsync("Dealing .");
        }

        public override async Task NextTurn()
        {
            await Channel.SendMessageAsync("Next turn commencing.");
            _turn++;
            _state = GameState.StartOfTurn;
        }

        public override async Task EndGame(string endmsg)
        {
            await base.EndGame(endmsg);
            await Channel.SendMessageAsync(endmsg);
        }

        public override string GetGameState()
        {
            var sb = new StringBuilder($"State of the game at turn {_turn}")
                .AppendLine($"The current turn player is **{TurnPlayer.Username}**.")
                .AppendLine($"The current phase is **{_state.ToString()}**");

            return sb.ToString();
        }

        private enum GameState
        {
            Setup,
            StartOfTurn,
            MainPhase,
            SpecialPhase,
            EndPhase
        }
    }
}

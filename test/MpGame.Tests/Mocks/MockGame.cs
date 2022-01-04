//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Addons.MpGame;

//namespace MpGame.Tests.Mocks
//{
//    public sealed class MockGame : GameBase<Player>
//    {
//        public MockGame(IMessageChannel channel, IEnumerable<Player> players)
//            : base(channel, players, false)
//        {
//        }

//        public override Task SetupGame() => throw new NotImplementedException();
//        public override Task StartGame() => throw new NotImplementedException();
//        public override Task NextTurn() => throw new NotImplementedException();
//        public override string GetGameState() => throw new NotImplementedException();
//        public override Embed GetGameStateEmbed() => throw new NotImplementedException();
//    }
//}

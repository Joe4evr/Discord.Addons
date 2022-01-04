//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Addons.MpGame;

//namespace MpGame.Tests.Mocks
//{
//    public sealed class MockModule : MpGameModuleBase<MockService, MockGame, Player>
//    {
//        public MockModule(MockService gameService)
//            : base(gameService)
//        {
//        }

//        public override Task CancelGameCmd() => throw new NotImplementedException();
//        public override Task EndGameCmd() => throw new NotImplementedException();
//        public override Task GameStateCmd() => throw new NotImplementedException();
//        public override Task JoinGameCmd() => throw new NotImplementedException();
//        public override Task LeaveGameCmd() => throw new NotImplementedException();
//        public override Task NextTurnCmd() => throw new NotImplementedException();
//        public override Task OpenGameCmd() => throw new NotImplementedException();
//        public override Task StartGameCmd() => throw new NotImplementedException();
//    }
//}

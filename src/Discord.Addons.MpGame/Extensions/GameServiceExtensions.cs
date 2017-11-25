//using System;
//using System.Collections.Generic;
//using System.Text;
//using Discord.Commands;

//namespace Discord.Addons.MpGame
//{
//    public static class GameServiceExtensions
//    {
//        public static void AddPlayerTypereader<TService, TGame, TPlayer>(this TService service, CommandService commandService)
//            where TService : MpGameService<TGame, TPlayer>
//            where TGame    : GameBase<TPlayer>
//            where TPlayer  : Player
//        {
//            commandService.AddTypeReader<TPlayer>(new PlayerTypeReader<TService, TGame, TPlayer>());
//        }
//    }
//}

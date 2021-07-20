//using System;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Commands;
//using Discord.Addons.Core;

//namespace Discord.Addons.MpGame
//{
//    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
//    {
//        //[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
//        protected abstract class MpGamePreconditionAttribute //: PreconditionAttribute
//        {
//            protected Result<MpGameService<TGame, TPlayer>.MpGameData> GetGameData(
//                ICommandContext context, IServiceProvider services)
//            {
//                var service = services.GetService<TService>();
//                if (service is null)
//                    return Result<MpGameService<TGame, TPlayer>.MpGameData>.Fault("Specified service not found.");

//                //var game = service.GetGameFromChannel(context.Channel);
//                //if (game is null)
//                //    return Result<TGame>.Fault("No game initialized.");

//                return Result.Success(service.GetGameData(context));
//            }
//        }
//    }
//}

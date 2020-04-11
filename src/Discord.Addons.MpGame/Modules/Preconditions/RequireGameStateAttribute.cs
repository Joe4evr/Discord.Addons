//using System;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Commands;

//namespace Discord.Addons.MpGame
//{
//    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
//    {
//        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
//        private sealed class RequireGameStateAttribute<TState> //: PreconditionAttribute
//            where TState : struct//, Enum
//        {
//            private readonly TState _state;
//
//            public RequireGameStateAttribute(TState state)
//            {
//                _state = state;
//            }
//
//            public /*override*/ Task<PreconditionResult> CheckPermissions(
//                ICommandContext context, CommandInfo _, IServiceProvider services)
//            {
//                var service = services.GetService<TService>();
//                if (service is null)
//                    return Task.FromResult(PreconditionResult.FromError("No service."));
//
//                var game = service.GetGameFromChannel(context.Channel);
//                if (game is null)
//                    return Task.FromResult(PreconditionResult.FromError("No game."));
//
//                }
//            }
//        }
//    }
//}

//using System;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Commands;

//namespace Discord.Addons.MpGame
//{
//    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
//    {
//        [AttributeUsage(AttributeTargets.Method)]
//        private sealed class RequireTurnPlayerAttribute : PreconditionAttribute
//        {
//            public override Task<PreconditionResult> CheckPermissions(
//                ICommandContext context,
//                CommandInfo command,
//                IServiceProvider services)
//            {
//                var service = services.GetService<TService>();
//                if (service != null)
//                {
//                    var game = service.GetGameFromChannel(context.Channel);
//                    if (game != null)
//                    {
//                        return (game.TurnPlayer.Value.User.Id == context.User.Id)
//                            ? Task.FromResult(PreconditionResult.FromSuccess())
//                            : Task.FromResult(PreconditionResult.FromError("Command can only be used by the turn player."));
//                    }
//                    return Task.FromResult(PreconditionResult.FromError("No game in progress."));
//                }
//                return Task.FromResult(PreconditionResult.FromError("No service."));
//            }
//        }
//    }
//}

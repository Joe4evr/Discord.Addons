//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Commands;

//namespace Discord.Addons.MpGame
//{
//    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
//    {
//        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
//        protected sealed class RequirePlayerAttribute : PreconditionAttribute
//        {
//            public override Task<PreconditionResult> CheckPermissionsAsync(
//                ICommandContext context, CommandInfo _, IServiceProvider services)
//            {
//                var service = services.GetService<TService>();
//                if (service is null)
//                    return Task.FromResult(PreconditionResult.FromError("Required service not found."));
//
//                var game = service.GetGameFromChannel(context.Channel);
//                if (game is null)
//                    return Task.FromResult(PreconditionResult.FromError("No game active."));
//
//                var authorId = context.User.Id;
//                return (game.Players.Select(p => p.User.Id).Contains(authorId))
//                    ? Task.FromResult(PreconditionResult.FromSuccess())
//                    : Task.FromResult(PreconditionResult.FromError("User must be a Player in this game."));
//            }
//        }
//    }
//}

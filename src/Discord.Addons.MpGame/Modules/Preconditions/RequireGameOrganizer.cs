//using System;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Commands;

//namespace Discord.Addons.MpGame
//{
//    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
//    {
//        //[AttributeUsage(AttributeTargets.Method)]
//        private sealed class RequireGameOrganizerAttribute //: PreconditionAttribute
//        {
//            public /*override*/ Task<PreconditionResult> CheckPermissions(
//                ICommandContext context,
//                CommandInfo command,
//                IServiceProvider services)
//            {
//                var service = services.GetService<TService>();
//                if (service != null)
//                {
//                    if (service.TryGetPersistentData(context.Channel, out var data))
//                    {
//                        return (data.GameOrganizer.Id == context.User.Id)
//                            ? Task.FromResult(PreconditionResult.FromSuccess())
//                            : Task.FromResult(PreconditionResult.FromError("Command can only be used by the user that intialized the game."));
//                    }
//                    return Task.FromResult(PreconditionResult.FromError("No game initialized."));
//                }
//                return Task.FromResult(PreconditionResult.FromError("No service."));
//            }
//        }
//    }
//}

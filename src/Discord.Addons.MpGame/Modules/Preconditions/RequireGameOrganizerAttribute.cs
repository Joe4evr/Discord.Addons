#if NET6_0_OR_GREATER
using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        private sealed class RequireGameOrganizerAttribute : GameStatePreconditionAttribute
        {
            protected override Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext context)
            {
                throw new NotImplementedException();
            }
            //public override Task<PreconditionResult> CheckPermissionsAsync(
            //    ICommandContext context, CommandInfo _, IServiceProvider services)
            //{
            //    var result = GetGameData(context, services);
            //    if (!result.IsSuccess(out var data))
            //        return Task.FromResult(PreconditionResult.FromError(result.Message));

            //    return (data.GameOrganizer.Id == context.User.Id)
            //        ? Task.FromResult(PreconditionResult.FromSuccess())
            //        : Task.FromResult(PreconditionResult.FromError("Command can only be used by the user that intialized the game."));
            //}
        }
    }
}
#endif

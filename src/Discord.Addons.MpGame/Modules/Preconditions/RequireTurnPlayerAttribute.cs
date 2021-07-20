#if NET6_0_OR_GREATER
using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
    {
        /// <summary>
        ///     Requires the user to be the turn player in the current game.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        protected sealed class RequireTurnPlayerAttribute : PreconditionAttribute
        {
            public override Task<PreconditionResult> CheckPermissionsAsync(
                ICommandContext context, CommandInfo _, IServiceProvider services)
            {
                var result = GetGameData(context, services);
                if (!result.IsSuccess(out var data))
                    return Task.FromResult(PreconditionResult.FromError(result.Message));

                return (data.Game is { } game && game.TurnPlayer.Value.User.Id == context.User.Id)
                    ? Task.FromResult(PreconditionResult.FromSuccess())
                    : Task.FromResult(PreconditionResult.FromError("Command can only be used by the turn player."));
            }
        }
    }
}
#endif

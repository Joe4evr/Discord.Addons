#if NET6_0_OR_GREATER
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
    {
        /// <summary>
        ///     Requires the user to be a player in the current game.
        /// </summary>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        protected sealed class RequirePlayerAttribute : PreconditionAttribute
        {
            public override Task<PreconditionResult> CheckPermissionsAsync(
                ICommandContext context, CommandInfo _, IServiceProvider services)
            {
                var result = GetGameData(context, services);
                if (!result.IsSuccess(out var data))
                    return Task.FromResult(PreconditionResult.FromError(result.Message));

                var authorId = context.User.Id;
                return (data.Game is { } game && game.Players.Select(p => p.User.Id).Contains(authorId))
                    ? Task.FromResult(PreconditionResult.FromSuccess())
                    : Task.FromResult(PreconditionResult.FromError("User must be a Player in this game."));
            }
        }
    }
}
#endif

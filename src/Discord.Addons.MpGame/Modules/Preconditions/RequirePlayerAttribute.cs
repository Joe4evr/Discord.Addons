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
        /// <remarks>
        ///     <inheritdoc />
        /// </remarks>
        //[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        protected sealed class RequirePlayerAttribute //: GameStatePreconditionAttribute
        {
            ///// <inheritdoc />
            //public override string? ErrorMessage { get; set; }

            //protected override Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext _)
            //{
            //    var authorId = context.User.Id;
            //    return (game.Players.Any(p => p.User.Id == authorId))
            //        ? Task.FromResult(PreconditionResult.FromSuccess())
            //        : Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? "User must be a Player in this game."));
            //}
        }
    }
}
#endif

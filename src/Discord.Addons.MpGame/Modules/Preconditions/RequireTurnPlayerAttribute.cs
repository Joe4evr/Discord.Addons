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
        /// <remarks>
        ///     <inheritdoc />
        /// </remarks>
        //[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        protected sealed class RequireTurnPlayerAttribute //: GameStatePreconditionAttribute
        {
            ///// <inheritdoc />
            //public override string? ErrorMessage { get; set; }

            //protected override Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext _)
            //{
            //    return (game.TurnPlayer.Value.User.Id == context.User.Id)
            //        ? Task.FromResult(PreconditionResult.FromSuccess())
            //        : Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? "Command can only be used by the turn player."));
            //}
        }
    }
}
#endif

#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
    {
        /// <summary>
        ///     Checks the current state of a game based on an <see langword="enum"/> property.
        /// </summary>
        /// <remarks>
        ///     Requires the Game type to implement
        ///     <see cref="ISimpleStateProvider{TState}"/>.
        /////     <inheritdoc />
        /// </remarks>
        /// <typeparam name="TState">
        ///     The state type. This must be an <see langword="enum"/> type
        ///     and must match the state type implemented from <see cref="ISimpleStateProvider{TState}"/>.
        /// </typeparam>
        //[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        protected sealed class RequireGameStateAttribute<TState> //: GameStatePreconditionAttribute
            where TState : struct, Enum
        {
            //public override string? ErrorMessage { get; set; }

            private readonly TState _requiredState;

            /// <summary>
            ///     Checks the current state of a game based on an <see langword="enum"/> property.
            /// </summary>
            /// <param name="state">
            ///     The required state.
            /// </param>
            public RequireGameStateAttribute(TState state)
            {
                _requiredState = state;
            }

            //protected override Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext _)
            //{
            //    return (game is ISimpleStateProvider<TState> stateProvider)
            //        ? (EqualityComparer<TState>.Default.Equals(stateProvider.State, _requiredState))
            //            ? Task.FromResult(PreconditionResult.FromSuccess())
            //            : Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? "Game was not in the correct state for this command."))
            //        : Task.FromResult(PreconditionResult.FromError("Game type must implement 'ISimpleStateProvider<TState>'."));
            //}
        }
    }
}
#endif

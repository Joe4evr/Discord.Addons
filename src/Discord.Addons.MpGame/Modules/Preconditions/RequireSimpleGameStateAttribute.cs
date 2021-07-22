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
        ///     Checks the current state of a game
        ///     based on an Enum property.
        /// </summary>
        /// <remarks>
        ///     Requires the Game type to implement
        ///     <see cref="ISimpleStateProvider{TState}"/>.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        protected sealed class RequireSimpleGameStateAttribute<TState> : GameStatePreconditionAttribute
            where TState : struct, Enum
        {
            private readonly TState _state;

            /// <summary>
            ///     Checks the current state of a game
            ///     based on an Enum property.
            /// </summary>
            /// <param name="state">
            ///     The required state.
            /// </param>
            public RequireSimpleGameStateAttribute(TState state)
            {
                _state = state;
            }

            protected override Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext _)
            {
                return (game is ISimpleStateProvider<TState> stateProvider)
                    ? (EqualityComparer<TState>.Default.Equals(stateProvider.State, _state))
                        ? Task.FromResult(PreconditionResult.FromSuccess())
                        : Task.FromResult(PreconditionResult.FromError("Game was not in the correct state for this command."))
                    : Task.FromResult(PreconditionResult.FromError("Game type must implement 'ISimpleStateProvider<TState>'."));
            }
        }
    }
}
#endif

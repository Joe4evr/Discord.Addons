#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
    {
        /// <summary>
        ///     Checks the current state of a game based
        ///     on an <see langword="enum"/> property.
        /// </summary>
        /// <remarks>
        ///     Requires the Game type to implement <see cref="ISimpleStateProvider{TState}"/>.<br/>
        ///     <note>
        ///         This precondition checks for exactly one given state.
        ///     </note><br/>
        ///     <note>
        ///         <inheritdoc/>
        ///     </note>
        /// </remarks>
        /// <typeparam name="TState">
        ///     The state type. This must be an <see langword="enum"/> type
        ///     and must match the state type implemented from <see cref="ISimpleStateProvider{TState}"/>.
        /// </typeparam>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        protected sealed class RequireGameStateAttribute<TState> : GameStatePreconditionAttribute
            where TState : struct, Enum
        {
            private static readonly string _stateType = typeof(TState).Name;

            public override string? ErrorMessage { get; set; }

            private readonly TState _requiredState;

            /// <summary>
            ///     Checks the current state of a game against
            ///     the given <see langword="enum"/> value.
            /// </summary>
            /// <param name="state">
            ///     The required state.
            /// </param>
            public RequireGameStateAttribute(TState state)
            {
                _requiredState = state;
            }

            protected override Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext _)
            {
                return (game is ISimpleStateProvider<TState> stateProvider)
                    ? (EqualityComparer<TState>.Default.Equals(stateProvider.State, _requiredState))
                        ? Task.FromResult(PreconditionResult.FromSuccess())
                        : Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? "Game was not in the correct state for this command."))
                    : Task.FromResult(PreconditionResult.FromError($"Game type must implement 'ISimpleStateProvider<{_stateType}>' to use this precondition."));
            }
        }

        /// <summary>
        ///     Checks the current state of a game based
        ///     on an <see langword="enum"/> property.
        /// </summary>
        /// <remarks>
        ///     Requires the Game type to implement <see cref="ISimpleStateProvider{TState}"/>.<br/>
        ///     <note>
        ///         This precondition checks for one out of several given states.
        ///     </note><br/>
        ///     <note>
        ///         <inheritdoc/>
        ///     </note>
        /// </remarks>
        /// <typeparam name="TState">
        ///     The state type. This must be an <see langword="enum"/> type
        ///     and must match the state type implemented from <see cref="ISimpleStateProvider{TState}"/>.
        /// </typeparam>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
        protected sealed class RequireGameStateOneOfAttribute<TState> : GameStatePreconditionAttribute
            where TState : struct, Enum
        {
            private static readonly string _stateType = typeof(TState).Name;

            public override string? ErrorMessage { get; set; }

            private readonly HashSet<TState> _requiredStates;

            /// <summary>
            ///     Checks the current state of a game against
            ///     one of several given <see langword="enum"/> values.
            /// </summary>
            /// <param name="states">
            ///     The required states.
            /// </param>
            public RequireGameStateOneOfAttribute(params TState[] states)
            {
                _requiredStates = new(states ?? Array.Empty<TState>());
            }

            protected override Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext _)
            {
                return (game is ISimpleStateProvider<TState> stateProvider)
                    ? (_requiredStates.Contains(stateProvider.State))
                        ? Task.FromResult(PreconditionResult.FromSuccess())
                        : Task.FromResult(PreconditionResult.FromError(ErrorMessage ?? "Game was not in a correct state for this command."))
                    : Task.FromResult(PreconditionResult.FromError($"Game type must implement 'ISimpleStateProvider<{_stateType}>' to use this precondition."));
            }
        }
    }
}
#endif

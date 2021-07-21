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
        ///     Base precondition to require a specific
        ///     state a game is in to execute the command.
        /// </summary>
        /// <remarks>
        ///     The simplest implementation of this is
        ///     <see cref="RequireSimpleGameStateAttribute{TState}"/>.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public abstract class GameStatePreconditionAttribute : PreconditionAttribute
        {
            public sealed override Task<PreconditionResult> CheckPermissionsAsync(
                ICommandContext context, CommandInfo _, IServiceProvider services)
            {
                var result = GetGameData(context, services);
                if (!result.IsSuccess(out var data))
                    return Task.FromResult(PreconditionResult.FromError(result.Message));

                return (data.Game is { } game)
                    ? CheckStateAsync(game, context)
                    : Task.FromResult(PreconditionResult.FromError("No game in progress."));
            }

            /// <summary>
            ///     Checks the current game's state.
            /// </summary>
            /// <param name="game">
            ///     The instance of a game in progress.
            /// </param>
            /// <param name="context">
            ///     The command context.
            /// </param>
            /// <returns>
            ///     The result of checking the state.
            /// </returns>
            protected abstract Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext context);
        }

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

        /// <summary>
        ///     Base precondition to require a command parameter
        ///     be valid given the state a game is in to execute the command.
        /// </summary>
        [AttributeUsage(AttributeTargets.Parameter)]
        public abstract class GameStateParameterPreconditionAttribute : ParameterPreconditionAttribute
        {
            public sealed override Task<PreconditionResult> CheckPermissionsAsync(
                ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
            {
                var result = GetGameData(context, services);
                if (!result.IsSuccess(out var data))
                    return Task.FromResult(PreconditionResult.FromError(result.Message));

                return (data.Game is { } game)
                    ? CheckValueAsync(game, value, context)
                    : Task.FromResult(PreconditionResult.FromError("No game in progress."));
            }
            
            /// <summary>
            ///     Checks the argument value against the current game's state.
            /// </summary>
            /// <param name="game">
            ///     The instance of a game in progress.
            /// </param>
            /// <param name="value">
            ///     The argument value.
            /// </param>
            /// <param name="context">
            ///     The command context.
            /// </param>
            /// <returns>
            ///     The result of checking the state.
            /// </returns>
            protected abstract Task<PreconditionResult> CheckValueAsync(TGame game, object value, ICommandContext context);
        }
    }
}
#endif

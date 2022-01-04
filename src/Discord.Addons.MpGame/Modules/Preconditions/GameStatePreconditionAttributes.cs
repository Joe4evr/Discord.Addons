#if NET6_0_OR_GREATER
using System;
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
        ///     <note type="warning">
        ///         This precondition is only available when
        ///         targeting .NET 6 or higher <em>and</em> your
        ///         project's &lt;LangVersion&gt; is set to
        ///         'preview' or a supported version.
        ///     </note>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public abstract class GameStatePreconditionAttribute : PreconditionAttribute
        {
            /// <summary>
            ///     The error message desired if the game service was not found.<br/>
            ///     If not provided, will use a default error message.
            /// </summary>
            public string? NoServiceError { get; init; }

            /// <summary>
            ///     The error message desired if there was no game data available.<br/>
            ///     If not provided, will use a default error message.
            /// </summary>
            public string? NoGameError { get; init; }

            public sealed override Task<PreconditionResult> CheckPermissionsAsync(
                ICommandContext context, CommandInfo _, IServiceProvider services)
            {
                var result = GetGameData(context, services, noSvcErr: NoServiceError, noGameErr: NoGameError);
                if (!result.IsSuccess(out var data))
                    return Task.FromResult(PreconditionResult.FromError(result.Message));

                return (data.Game is { } game)
                    ? CheckStateAsync(game, context)
                    : Task.FromResult(PreconditionResult.FromError(NoGameError ?? "No game in progress."));
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
        ///     Base precondition to require a command parameter
        ///     be valid given the state a game is in to execute the command.
        /// </summary>
        /// <remarks>
        ///     <note type="warning">
        ///         This precondition is only available when
        ///         targeting .NET 6 or higher <em>and</em> your
        ///         project's &lt;LangVersion&gt; is set to
        ///         'preview' or a supported version.
        ///     </note>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Parameter)]
        public abstract class GameStateParameterPreconditionAttribute : ParameterPreconditionAttribute
        {
            /// <summary>
            ///     The error message desired if the game service was not found.<br/>
            ///     If not provided, will use a default error message.
            /// </summary>
            public string? NoServiceError { get; init; }

            /// <summary>
            ///     The error message desired if there was no game data available.<br/>
            ///     If not provided, will use a default error message.
            /// </summary>
            public string? NoGameError { get; init; }

            public sealed override Task<PreconditionResult> CheckPermissionsAsync(
                ICommandContext context, ParameterInfo _, object? value, IServiceProvider services)
            {
                var result = GetGameData(context, services, noSvcErr: NoServiceError, noGameErr: NoGameError);
                if (!result.IsSuccess(out var data))
                    return Task.FromResult(PreconditionResult.FromError(result.Message));

                return (data.Game is { } game)
                    ? CheckValueAsync(game, value, context)
                    : Task.FromResult(PreconditionResult.FromError(NoGameError ?? "No game in progress."));
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
            protected abstract Task<PreconditionResult> CheckValueAsync(TGame game, object? value, ICommandContext context);
        }
    }
}
#endif

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Discord.Addons.MpGame;

public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
{
    private sealed class PlayerTypeReader : UserTypeReader<IUser>
    {
        /// <inheritdoc/>
        public override async Task<TypeReaderResult> ReadAsync(
            ICommandContext context, string input, IServiceProvider services)
        {
            var result = await base.ReadAsync(context, input, services).ConfigureAwait(false);
            if (!result.IsSuccess)
                return result;

            var user = (result.BestMatch as IUser)
                ?? await TryDownloadUserAsync(context.Client, input);
            if (user is null)
                return TypeReaderResult.FromError(CommandError.ObjectNotFound, "User not found.");

            var gameResult = GetGameData(context, services, "Game service not found.", null);
            if (!gameResult.IsSuccess(out var gameData))
                return TypeReaderResult.FromError(CommandError.ObjectNotFound, gameResult.Message);

            if (gameData.Game is not { } game)
                return TypeReaderResult.FromError(CommandError.ObjectNotFound, "No game going on.");

            var player = game.Players.SingleOrDefault(p => p.User.Id == user.Id);
            return (player is null)
                ? TypeReaderResult.FromError(CommandError.ObjectNotFound, "Specified user not a player in this game.")
                : TypeReaderResult.FromSuccess(player);
        }

        private static async Task<IUser?> TryDownloadUserAsync(
            IDiscordClient client, string input)
        {
            if (!UInt64.TryParse(input, out var uid))
            {
                if (!MentionUtils.TryParseUser(input, out uid))
                    return null;
            }

            return client switch
            {
                DiscordRestClient rest => await rest.GetUserAsync(uid),
                DiscordSocketClient sock => await sock.Rest.GetUserAsync(uid),
                _ => null
            };
        }
    }
}

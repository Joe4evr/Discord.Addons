using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
    {
        private sealed class PlayerTypeReader : UserTypeReader<IUser>
        {
            /// <inheritdoc/>
            public override async Task<TypeReaderResult> ReadAsync(
                ICommandContext context, string input, IServiceProvider services)
            {
                var result = await base.ReadAsync(context, input, services).ConfigureAwait(false);
                if (!(result.IsSuccess && result.BestMatch is IUser user))
                    return result;

                var svc = services.GetService<TService>();
                if (svc is null)
                    return TypeReaderResult.FromError(CommandError.ObjectNotFound, "Game service not found.");

                var game = svc.GetGameFromChannel(context.Channel);
                if (game is null)
                    return TypeReaderResult.FromError(CommandError.ObjectNotFound, "No game going on.");

                var player = game.Players.SingleOrDefault(p => p.User.Id == user.Id);
                return (player is null)
                    ? TypeReaderResult.FromError(CommandError.ObjectNotFound, "Specified user not a player in this game.")
                    : TypeReaderResult.FromSuccess(player);
            }
        }
    }
}

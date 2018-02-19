using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace Discord.Addons.MpGame
{
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
    {
        //void AddTypeReader<TType>(
        //    TypeReader reader,
        //    Func<ParameterInfo, bool> predicate = null)
        //{
        //}

        internal sealed class PlayerTypeReader : TypeReader
        {
            private static readonly Regex _mentionParser = new Regex(@"^<@!?(?<digits>\d+)>$", RegexOptions.Compiled);
            private static readonly Regex _nameDiscrimParser = new Regex(@"^(?<name>.*)#(?<discrim>\d{4})$", RegexOptions.Compiled);

            public override async Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
            {
                var idMatch = _mentionParser.Match(input);
                if (!idMatch.Success || !UInt64.TryParse(idMatch.Groups["digits"].Value, out var id))
                {
                    var nameDiscrimMatch = _nameDiscrimParser.Match(input);
                    if (!nameDiscrimMatch.Success)
                    {
                        return TypeReaderResult.FromError(CommandError.ParseFailed, "Could not parse input.");
                    }
                    id = (await context.Client.GetUserAsync(nameDiscrimMatch.Groups["name"].Value, nameDiscrimMatch.Groups["discrim"].Value)).Id;
                }

                var svc = services.GetService<TService>();
                if (svc != null)
                {
                    var game = svc.GetGameFromChannel(context.Channel);
                    if (game != null)
                    {
                        var player = game.Players.SingleOrDefault(p => p.User.Id == id);
                        if (player != null)
                        {
                            return TypeReaderResult.FromSuccess(player);
                        }
                        return TypeReaderResult.FromError(CommandError.ObjectNotFound, "Not a player in this game.");
                    }
                    return TypeReaderResult.FromError(CommandError.ObjectNotFound, "No game going on.");
                }
                return TypeReaderResult.FromError(CommandError.ObjectNotFound, "No service found.");
            }
        }
    }
}

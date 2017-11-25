//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Commands;

//namespace Discord.Addons.MpGame
//{
//    internal sealed class PlayerTypeReader<TService, TGame, TPlayer> : TypeReader
//        where TService : MpGameService<TGame, TPlayer>
//        where TGame : GameBase<TPlayer>
//        where TPlayer : Player
//    {
//        private static readonly Regex _idParser = new Regex(@"<!?(?<digits>\d+)>", RegexOptions.Compiled);

//        public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
//        {
//            var match = _idParser.Match(input);
//            if (!match.Success || !UInt64.TryParse(match.Groups["digits"].Value, out var id))
//            {
//                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Could not parse input."));
//            }

//            var svc = services.GetService<TService>();
//            if (svc != null)
//            {
//                var game = svc.GetGameFromChannel(context.Channel);
//                if (game != null)
//                {
//                    var player = game.Players.SingleOrDefault(p => p.User.Id == id);
//                    if (player != null)
//                    {
//                        return Task.FromResult(TypeReaderResult.FromSuccess(player));
//                    }
//                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ObjectNotFound, "Not a player in that game."));
//                }
//                return Task.FromResult(TypeReaderResult.FromError(CommandError.ObjectNotFound, "No game."));
//            }
//            return Task.FromResult(TypeReaderResult.FromError(CommandError.ObjectNotFound, "No service."));
//        }
//    }
//}

#if NET6_0_OR_GREATER
using System;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame
{
    public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
    {
        private static Result<MpGameService<TGame, TPlayer>.MpGameData> GetGameData(
                ICommandContext context, IServiceProvider services)
        {
            var service = services.GetService<TService>();
            if (service is null)
                return Result<MpGameService<TGame, TPlayer>.MpGameData>.Fault($"Service type '{typeof(TService)}' not found.");

            var gameData = service.GetGameData(context);
            if (gameData is null)
                return Result<MpGameService<TGame, TPlayer>.MpGameData>.Fault("No game in progress.");

            return Result.Success(gameData);
        }
    }
}
#endif

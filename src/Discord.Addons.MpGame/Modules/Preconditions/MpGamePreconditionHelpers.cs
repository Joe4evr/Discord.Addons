using System;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame;

public abstract partial class MpGameModuleBase<TService, TGame, TPlayer>
{
    private static Result<MpGameService<TGame, TPlayer>.MpGameData> GetGameData(
            ICommandContext context, IServiceProvider services,
            string? noSvcErr, string? noGameErr)
    {
        var service = services.GetService<TService>();
        if (service is null)
            return Result<MpGameService<TGame, TPlayer>.MpGameData>.Fault(noSvcErr ?? $"Service type '{typeof(TService)}' not found.");

        var gameData = service.GetGameData(context);
        if (gameData is null)
            return Result<MpGameService<TGame, TPlayer>.MpGameData>.Fault(noGameErr ?? "No game data available.");

        return Result.Success(gameData);
    }
}

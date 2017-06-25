Extra considerations
====================

For your Game logic, you will likely want to limit some commands
to certain times or players. This can be accomplished using Preconditions.

For example, if a certain command can only be used by the Turn Player,
you will need to make a class like this:
```cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class RequireTurnPlayerAttribute : PreconditionAttribute
{
    public async override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        var service = services.GetService<CardGameService>();
        if (service != null)
        {
            var authorId = context.User.Id;
            // Use this handy method to retrieve the Game instance going on, if any
            var game = await service.GetGameFromChannelAsync(context.Channel);

            if (game != null)
            {
                return (game.TurnPlayer.Value.User.Id == authorId)
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError("Cannot use command at this time.");
            }
            return PreconditionResult.FromError("No game active.");
        }
        return PreconditionResult.FromError("No service found.");
    }
}
```

You can then apply `[RequireTurnPlayer]` on any Command method in your Module that
is only allowed to be used by the Turn Player. Other preconditions, for example,
checking if the game is in a particular state, can be made in a similar way.


[<- Part 6 - Final step](6-FinalStep.md) - Extra considerations
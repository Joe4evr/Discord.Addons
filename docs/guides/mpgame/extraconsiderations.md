---
uid: Addons.MpGame.Extra
title: Extra considerations
---
## Extra considerations

For your Game logic, you will likely want to limit some commands
to certain times or players. This can be accomplished using Preconditions.

For example, if a certain command can only be used by the Turn Player,
you will need to make a class like this:
```cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class RequireTurnPlayerAttribute : PreconditionAttribute
{
  public override Task<PreconditionResult> CheckPermissions(
    ICommandContext context, CommandInfo command, IServiceProvider services)
  {
    var service = services.GetService<CardGameService>();
    if (service is null)
      return Task.FromResult(PreconditionResult.FromError("Required service not found."));
    
    // Use this handy method to retrieve the Game instance going on, if any
    var game = service.GetGameFromChannel(context.Channel);
    if (game is null)
      return Task.FromResult(PreconditionResult.FromError("No game active."));
    
    return (game.TurnPlayer.Value.User.Id == context.User.Id)
      ? Task.FromResult(PreconditionResult.FromSuccess())
      : Task.FromResult(PreconditionResult.FromError("Cannot use command at this time."));
  }
}
```

You can then apply `[RequireTurnPlayer]` on any Command method in your Module that
is only allowed to be used by the Turn Player. Other preconditions, for example,
checking if the game is in a particular state, can be made in a similar way.

---
uid: Addons.MpGame.Preconditions
title: Preconditions
---
## Preconditions

Starting with v4, when targeting .NET 6 or greater and using C# 10 or higher, a number of common preconditions can be used out of the box, through the magic of Generic Attributes. These preconditions are provided from the `MpGameModuleBase<>` class and include:

* `[RequirePlayer]`: A command can only be invoked by __a__ player in the current game.
* `[RequireTurnPlayer]`: A command can only be invoked by __the turn player__ in the current game.
* `[RequireGameState<TState>]`: A command can only be invoked if the current game is in a particular state as determined by an enum property. This precondition requires that your game type also implements `ISimpleStateProvider<TState>`.


All of these have the same base type, `GameStatePreconditionAttribute`, which you can also use to write a custom precondition.
```cs
// It's easier to list the base type here through your own module type,
// so that you don't have to write out all the type arguments.
public sealed class RequireElapsedTurnsAttribute : CardGameModule.GameStatePreconditionAttribute
{
  private readonly int _minTurns;
  
  public RequireElapsedTurnsAttribute(int minTurns) => _minTurns = minTurns;
  
  protected override Task<PreconditionResult> CheckStateAsync(TGame game, ICommandContext context)
  {
      return (game.Turns >= _minTurns)
          ? Task.FromResult(PreconditionResult.FromSuccess())
          : Task.FromResult(PreconditionResult.FromError("Not enough turns have elapsed yet."));
  }
}
```

There is also a variation for parameter preconditions; `GameStateParameterPreconditionAttribute`. There are no implementations included, but it's not hard to build one:
```cs
public sealed class RequireLessThanCurrentDeckSizeAttribute : CardGameModule.GameStateParameterPreconditionAttribute
{
    protected override Task<PreconditionResult> CheckValueAsync(TGame game, object value, ICommandContext context)
    {
        return (value is uint num && num <= game.DeckSize)
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("Value out of range."));
    }
}
```


The built-in preconditions can be easily applied when inside your module class:
```cs
public class CardGameModule : MpGameModuleBase<CardGameService, CardGame, CardPlayer>
{
    [Command("demo")]
    [RequireTurnPlayer] // Restrict to usage only by the turn player
    [RequireSimpleGameState<CardGameState>(CardGameState.StartOfTurn)] // Restrict to usage only when it's the start of the turn
    public Task SomeRestrictedCommand()
    {
        //....
    }
}
```

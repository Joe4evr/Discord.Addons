---
uid: Addons.MpGame.Preconditions
title: Preconditions
---
## Preconditions

Starting with v4, when targeting .NET 6 or greater and using C# 10 or higher, a number of common preconditions can be used out of the box, through the magic of Generic Attributes. These preconditions are provided from the `MpGameModuleBase<>` class and include:

* `[RequirePlayer]`: A command can only be invoked by a player in the current game.
* `[RequireTurnPlayer]`: A command can only be invoked by the turn player in the current game.
* `[GameStateBase]`: A command can only be invoked if the current game is in a specific state. This precondition is abstract, but one concrete implementation is included:
  * `[RequireSimpleGameState<TState>]`: Determines the current game state by an enum property. This requires that your game type also implements `ISimpleStateProvider<TState>`.
  * If you wish to determine the current state in a custom way, you can write your own logic in a class that inherits from `[GameStateBase]`:
  ```cs
  // It's easier to list the base type here through your own module type,
  // so that you don't have to write out all the type arguments.
  public sealed class RequireElapsedTurnsAttribute : CardGameModule.GameStateBaseAttribute
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
* `[GameStateParameterBase]`: A variation of the `GameStateBaseAttribute` for parameter preconditions. This precondition is also abstract. There are no implementations included, but it's not hard to build one:
```cs
public sealed class RequireLessThanCurrentDeckSizeAttribute : CardGameModule.GameStateParameterBaseAttribute
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

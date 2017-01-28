Final step
==========

The final step is to register the game into the command service.
```cs
// on your IDependencyMap instance:
map.Add(new CardGameService());

// on your CommandService instance:
await commands.AddModule<CardGameModule>();
```

Another option is making an extension method like this:
```cs
public static class CardGameExt
{
    public static Task AddCardGame(this CommandService cmds, IDependencyMap map)
    {
        // Additional advantage: If you need to add anything else in particular
        // (such as a custom TypeReader), you can add that here as well
        map.Add(new CardGameService());
        return cmds.AddModule<CardGameModule>();
    }
}
```

Then you can simply call:
```cs
await commands.AddCardGame(map);
```

[<- Part 5 - Modules](5-Modules.md) - Final step - [Part 7 - Extra considerations](7-ExtraConsiderations.md)

Final step
==========

The final step is to register the game into the command service.
```cs
//on your IDependencyMap instance:
map.Add(new CardGameService());

//on your CommandService instance:
await commands.AddModule<CardGameModule>();
```

[<- Part 5 - Modules](5-Modules.md) - Final step

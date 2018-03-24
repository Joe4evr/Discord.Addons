Final step
==========

The final step is to register the game into the command service.
```cs
// on your IServiceCollection instance:
map.AddSingleton(new CardGameService());

// on your CommandService instance:
await commands.AddModule<CardGameModule>(_services);
```

[<- Part 5 - Modules](5-Modules.md) - Final step - [Part 7 - Extra considerations ->](7-ExtraConsiderations.md)

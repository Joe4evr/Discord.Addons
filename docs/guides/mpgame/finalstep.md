---
uid: Addons.MpGame.Final
title: Final step
---
## Final step

The final step is to register the service into the DI container
and the game module into the command service.
```cs
// on your IServiceCollection instance:
map.AddSingleton(new CardGameService());

// on your CommandService instance:
await commands.AddModule<CardGameModule>(_services);
```

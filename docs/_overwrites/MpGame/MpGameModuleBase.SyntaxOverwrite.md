---
uid: Discord.Addons.MpGame.MpGameModuleBase`3
syntax:
  content: |
    public class MpGameModuleBase<TService, TGame, TPlayer> : ModuleBase<SocketCommandContext>
        where TService : MpGameService<TGame, TPlayer>
        where TGame    : GameBase<TPlayer>
        where TPlayer  : Player
---
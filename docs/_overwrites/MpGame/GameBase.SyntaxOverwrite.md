---
uid: Discord.Addons.MpGame.GameBase`1
syntax:
  content: |
    public abstract class GameBase<TPlayer>
        where TPlayer : Player
---
uid: Discord.Addons.MpGame.GameBase`1.#ctor(Discord.IMessageChannel,System.Collections.Generic.IEnumerable{`0},System.Boolean)
syntax:
  content: |
    protected GameBase(IMessageChannel channel, IEnumerable<TPlayer> players,
        bool setFirstPlayerImmediately = false)
---

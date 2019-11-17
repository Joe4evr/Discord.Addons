---
uid: Discord.Addons.MpGame.Collections.WrappingPile`2
syntax:
  content: |
    public abstract class WrappingPile<T, TWrapper> : Pile<T>
        where T        : class
        where TWrapper : struct, IWrapper<T>
---
---
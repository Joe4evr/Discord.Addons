---
uid: Discord.Addons.MpGame.Collections.Pile`1.BrowseAndTakeAsync(System.Func{System.Collections.Generic.IReadOnlyDictionary{System.Int32,`0},System.Threading.Tasks.Task{System.Int32[]}},System.Func{`0,System.Boolean},System.Boolean)
syntax:
  content: |
    public Task<ImmutableArray<T>> BrowseAndTakeAsync(
        Func<IReadOnlyDictionary<int, T>, Task<int[]>> selector,
        Func<T, bool>? filter = null,
        bool shuffle = false)
---
---
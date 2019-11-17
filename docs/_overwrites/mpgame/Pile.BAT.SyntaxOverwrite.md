---
uid: Discord.Addons.MpGame.Collections.Pile`1.BrowseAndTakeAsync(System.Func{System.Collections.Generic.IReadOnlyDictionary{System.Int32,`0},System.Threading.Tasks.Task{System.Int32[]}},System.Func{`0,System.Boolean},System.Func{System.Collections.Immutable.ImmutableArray{`0},System.Collections.Generic.IEnumerable{`0}})
syntax:
  content: |
    public Task<ImmutableArray<T>> BrowseAndTakeAsync(
        Func<IReadOnlyDictionary<int, T>, Task<int[]>> selector,
        Func<T, bool>? filter = null,
        Func<ImmutableArray<T>, IEnumerable<T>>? shuffleFunc = null)
---
---
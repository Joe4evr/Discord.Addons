---
uid: Discord.Addons.MpGame.Collections.Pile`2.BrowseAndTake(System.Func{System.Collections.Immutable.ImmutableDictionary{System.Int32,`0},System.Threading.Tasks.Task{System.Int32[]}},System.Func{`0,System.Boolean},System.Func{System.Collections.Immutable.ImmutableArray{`0},System.Collections.Generic.IEnumerable{`0}})
syntax:
  content: |
    public Task<ImmutableArray<TCard>> BrowseAndTake(
        Func<ImmutableDictionary<int, TCard>, Task<int[]>> selector,
        Func<TCard, bool> filter = null,
        Func<ImmutableArray<TCard>, IEnumerable<TCard>> shuffleFunc = null)
---
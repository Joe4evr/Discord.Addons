using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame.Collections
{
    internal sealed class PileLogic<TIn, TOut>
    {
        private int _count = 0;
        private Node? _head = null;
        private Node? _tail = null;

        public int VCount => Volatile.Read(ref _count);
        private Node? VHead => Volatile.Read(ref _head);
        private Node? VTail => Volatile.Read(ref _tail);

        public IEnumerable<TOut> AsEnumerable(Func<TIn, TOut> unwrapper)
        {
            for (var n = VHead; n != null; n = n.Next)
            {
                yield return unwrapper(n.Value);
            }
        }

        public ImmutableArray<TOut> Browse(Func<TIn, TOut> unwrapper)
            => GetAllInternal(clear: false, unwrapper);

        public async Task<ImmutableArray<TOut>> BrowseAndTakeAsync(
            Func<IReadOnlyDictionary<int, TOut>, Task<int[]>> selector,
            Func<TOut, bool>? filter,
            Func<IEnumerable<TOut>, IEnumerable<TOut>> shuffleFunc,
            Func<TIn, TOut> unwrapper, Func<TOut, TIn> wrapper,
            bool shouldShuffle)
        {
            var items = GetAllDictionary(unwrapper);
            var imm = Freeze(
                (filter != null ? items.Where(kv => filter(kv.Value)) : items),
                VCount);

            var selection = await selector(imm).ConfigureAwait(false);
            var nodes = BuildSelection(this, selection, items, imm);

            if (shouldShuffle)
            {
                Resequence(shuffleFunc(ImmutableArray.CreateRange(items.Values)), wrapper);

                return ImmutableArray.CreateRange(nodes.Select(n => unwrapper(n.Value)));
            }
            else
            {
                return ImmutableArray.CreateRange(nodes.Select(n => unwrapper(Break(n))));
            }

            static Node[] BuildSelection(PileLogic<TIn, TOut> @this,
                int[] sel, Dictionary<int, TOut> cs,
                IReadOnlyDictionary<int, TOut> ics)
            {
                if (sel is null || sel.Length == 0)
                    return Array.Empty<Node>();

                var un = sel.Distinct().ToArray();
                var ex = un.Except(ics.Keys);
                if (ex.Any())
                    ThrowHelper.ThrowIndexOutOfRange($"Selected indeces '{String.Join(", ", ex)}' must be one of the provided item indices.");

                var arr = new Node[un.Length];

                for (int i = 0; i < un.Length; i++)
                {
                    var s = un[i];
                    arr[i] = @this.GetNodeAt(s);
                    cs.Remove(s);
                }

                return arr;
            }
        }

        public ImmutableArray<TOut> Clear(Func<TIn, TOut> unwrapper)
            => GetAllInternal(clear: true, unwrapper);

        public void Cut(int amount)
        {
            var oldHead = VHead!;
            var oldTail = VTail!;
            var newHead = GetNodeAt(amount);
            var newTail = newHead.Previous!;

            oldHead.Previous = oldTail;
            oldTail.Next = oldHead;
            newHead.Previous = null!;
            newTail.Next = null!;

            Volatile.Write(ref _head, newHead);
            Volatile.Write(ref _tail, newTail);
        }

        public TIn Draw()
            => Break(RemHead());

        public TIn DrawBottom()
            => Break(RemTail());

        public ImmutableArray<TOut> MultiDraw(int amount, Func<TIn, TOut> unwrapper)
        {
            var builder = ImmutableArray.CreateBuilder<TOut>(amount);
            var node = VHead!;
            var newHead = _head = GetNodeAt(amount);

            for (; !ReferenceEquals(node, newHead); node = node.Next!)
                builder.Add(unwrapper(Break(node)));

            return builder.MoveToImmutable();
        }

        public void InsertAt(TIn item, int index)
        {
            if (index == 0)
                AddHead(item);
            else if (index == VCount)
                AddTail(item);
            else
                AddAfter(GetNodeAt(index), item);
        }

        public TOut Mill(Func<TIn, TOut> unwrapper, Action<TOut> targetAdder)
        {
            var millNode = Break(RemHead());
            var item = unwrapper(millNode);
            targetAdder(item);
            return item;
        }

        public TIn PeekAt(int index)
            => GetNodeAt(index).Value;

        public ImmutableArray<TOut> PeekTop(int amount, Func<TIn, TOut> unwrapper)
        {
            var builder = ImmutableArray.CreateBuilder<TOut>(amount);

            for (var (n, i) = (VHead, 0); i < amount; (n, i) = (n.Next, i + 1))
                builder.Add(unwrapper(n!.Value)); // Nodes are kept in place for peeking

            return builder.MoveToImmutable();
        }

        public void Put(TIn item)
            => AddHead(item);

        public void PutBottom(TIn item)
            => AddTail(item);

        public void Shuffle(
            Func<IEnumerable<TOut>, IEnumerable<TOut>> shuffleFunc,
            Func<TIn, TOut> unwrapper, Func<TOut, TIn> wrapper)
            => Resequence(shuffleFunc(GetAllInternal(clear: false, unwrapper)), wrapper);

        public TIn TakeAt(int index)
            => Break(GetNodeAt(index));

        private Dictionary<int, TOut> GetAllDictionary(Func<TIn, TOut> unwrapper)
        {
            var res = new Dictionary<int, TOut>(capacity: VCount);
            if (VCount > 0)
            {
                for (var (n, i) = (VHead, 0); n != null; (n, i) = (n.Next, i + 1))
                {
                    var tmp = n.Value;
                    res.Add(i, unwrapper(tmp));
                }
            }
            return res;
        }
        private ImmutableArray<TOut> GetAllInternal(bool clear, Func<TIn, TOut> unwrapper)
        {
            if (VCount == 0)
                return ImmutableArray<TOut>.Empty;

            var builder = ImmutableArray.CreateBuilder<TOut>(VCount);

            for (var n = VHead; n != null; n = n.Next)
            {
                var tmp = n.Value;
                builder.Add(unwrapper(tmp));
            }

            if (clear)
            {
                Reset();
            }

            return builder.ToImmutable();
        }
        private void Resequence(IEnumerable<TOut> newSequence, Func<TOut, TIn> wrapper)
        {
            if (newSequence is null)
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NewSequenceNull);

            Reset();

            AddSequence(newSequence, wrapper);
        }
        public void AddSequence(IEnumerable<TOut> sequence, Func<TOut, TIn> wrapper)
        {
            foreach (var item in sequence.Distinct(ReferenceComparer<TOut>.Instance))
            {
                if (item != null)
                    AddTail(wrapper(item));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CountIncOne()
            => Interlocked.Increment(ref _count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CountDecOne()
            => Interlocked.Decrement(ref _count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reset()
        {
            Volatile.Write(ref _head, null);
            Volatile.Write(ref _tail, null);
            Volatile.Write(ref _count, 0);
        }

        private Node GetNodeAt(int index)
        {
            if (index == 0)
                return VHead!;
            if (index == VCount - 1)
                return VTail!;

            var tmp = VHead!;
            for (int i = 0; i < index; i++)
                tmp = tmp.Next!;

            return tmp;
        }
        internal void AddHead(TIn item)
        {
            var node = new Node(item);
            var oldHead = Interlocked.Exchange(ref _head, node);
            Interlocked.CompareExchange(ref _tail, value: node, comparand: null!);
            CountIncOne();
            node.Next = oldHead;

            if (oldHead != null)
                oldHead.Previous = node;
        }
        private void AddTail(TIn item)
        {
            var node = new Node(item);
            var oldTail = Interlocked.Exchange(ref _tail, node);
            Interlocked.CompareExchange(ref _head, value: node, comparand: null!);
            CountIncOne();
            node.Previous = oldTail;

            if (oldTail != null)
                oldTail.Next = node;
        }
        private void AddAfter(Node node, TIn item)
        {
            var tmp = new Node(item)
            {
                Next = node.Next,
                Previous = node
            };

            node.Next = tmp;
            CountIncOne();
        }
        private Node RemHead()
        {
            var headNode = Interlocked.Exchange(ref _head, _head?.Next);
            if (headNode is null)
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.PileEmpty);

            Interlocked.CompareExchange(ref _tail, value: null!, comparand: headNode);
            if (VHead != null)
                VHead.Previous = null!;

            return headNode;
        }
        private Node RemTail()
        {
            var tailNode = Interlocked.Exchange(ref _tail, _tail?.Previous);
            if (tailNode is null)
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.PileEmpty);

            Interlocked.CompareExchange(ref _head, value: null!, comparand: tailNode);
            if (VHead != null)
                VHead.Previous = null!;

            return tailNode;
        }
        private TIn Break(Node node)
        {
            Interlocked.CompareExchange(ref _head!, value: _head?.Next, comparand: node);
            Interlocked.CompareExchange(ref _tail!, value: _tail?.Previous, comparand: node);

            if (node.Previous != null)
                node.Previous.Next = node.Next;
            if (node.Next != null)
                node.Next.Previous = node.Previous!;

            CountDecOne();

            return node.Value;
        }

        internal ref TIn GetValueRefAt(int index)
            => ref GetNodeAt(index).Value;

        private sealed class Node
        {
            private TIn _value;

            internal Node? Next { get; set; }
            internal Node? Previous { get; set; }
            internal ref TIn Value => ref _value;

            internal Node(TIn value)
            {
                _value = value;
            }
        }

        private static ReadOnlyDictionary<TKey, TValue> Freeze<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values, int cap)
            where TKey : notnull
        {
            var d = new Dictionary<TKey, TValue>(capacity: cap);
            foreach (var kvp in values)
                d.Add(kvp.Key, kvp.Value);

            return new ReadOnlyDictionary<TKey, TValue>(d);
        }
    }
}

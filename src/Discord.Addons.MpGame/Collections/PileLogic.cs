using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame.Collections
{
    internal sealed class PileLogic<T>
    {
        private int _count = 0;
        private Node _head = null;
        private Node _tail = null;

        public int VCount => Volatile.Read(ref _count);
        private Node VHead => Volatile.Read(ref _head);
        private Node VTail => Volatile.Read(ref _tail);

        public IEnumerable<TOut> AsEnumerable<TOut>(Func<T, TOut> unwrapper)
        {
            for (var n = VHead; n != null; n = n.Next)
            {
                yield return unwrapper(n.Value);
            }
        }

        public ImmutableArray<TOut> Browse<TOut>(Func<T, TOut> unwrapper)
            => GetAllInternal(clear: false, unwrapper);

        public async Task<ImmutableArray<TOut>> BrowseAndTakeAsync<TOut>(
            Func<IReadOnlyDictionary<int, TOut>, Task<int[]>> selector,
            Func<TOut, bool> filter,
            Func<ImmutableArray<TOut>, IEnumerable<T>> shuffleFunc,
            Func<T, TOut> unwrapper,
            bool canShuffle)
        {
            var items = GetAllDictionary(unwrapper);
            var imm = Freeze(
                (filter != null ? items.Where(kv => filter(kv.Value)) : items),
                VCount);

            var selection = await selector(imm).ConfigureAwait(false);
            var nodes = BuildSelection(selection, items, imm);

            if (canShuffle && shuffleFunc != null)
            {
                Resequence(shuffleFunc(ImmutableArray.CreateRange(items.Values)));

                return ImmutableArray.CreateRange(nodes.Select(n => unwrapper(n.Value)));
            }
            else
            {
                return ImmutableArray.CreateRange(nodes.Select(n => unwrapper(Break(n))));
            }

            Node[] BuildSelection(int[] sel, Dictionary<int, TOut> cs, IReadOnlyDictionary<int, TOut> ics)
            {
                if (sel == null)
                    return Array.Empty<Node>();

                var un = sel.Distinct().ToArray();
                if (un.Length == 0)
                    return Array.Empty<Node>();

                var ex = un.Except(ics.Keys);
                if (ex.Any())
                    ThrowHelper.ThrowIndexOutOfRange($"Selected indeces '{String.Join(", ", ex)}' must be one of the provided item indices.");

                var arr = new Node[un.Length];

                for (int i = 0; i < un.Length; i++)
                {
                    var s = un[i];
                    arr[i] = GetNodeAt(s);
                    cs.Remove(s);
                }

                return arr;
            }
        }

        public ImmutableArray<TOut> Clear<TOut>(Func<T, TOut> unwrapper)
            => GetAllInternal(clear: true, unwrapper);

        public void Cut(int amount)
        {
            var oldHead = VHead;
            var oldTail = VTail;
            var newHead = GetNodeAt(amount);
            var newTail = newHead.Previous;

            oldHead.Previous = oldTail;
            oldTail.Next = oldHead;
            newHead.Previous = null;
            newTail.Next = null;

            Volatile.Write(ref _head, newHead);
            Volatile.Write(ref _tail, newTail);
        }

        public T Draw()
            => Break(RemHead());

        public T DrawBottom()
            => Break(RemTail());

        public void InsertAt(T item, int index)
        {
            if (index == 0)
                AddHead(item);
            else if (index == VCount)
                AddTail(item);
            else
                AddAfter(GetNodeAt(index), item);
        }

        public TOut Mill<TOut>(Func<T, TOut> unwrapper, Action<TOut> targetAdder)
        {
            var millNode = RemHead();
            CountDecOne();
            var item = unwrapper(millNode.Value);
            targetAdder(item);
            return item;
        }

        public T PeekAt(int index)
            => GetNodeAt(index).Value;

        public ImmutableArray<TOut> PeekTop<TOut>(int amount, Func<T, TOut> unwrapper)
        {
            var builder = ImmutableArray.CreateBuilder<TOut>(amount);

            for (var (n, i) = (VHead, 0); i < amount; (n, i) = (n.Next, i + 1))
                builder.Add(unwrapper(n.Value));

            return builder.ToImmutable();
        }

        public void Put(T item)
            => AddHead(item);

        public void PutBottom(T item)
            => AddTail(item);

        public void Shuffle<TOut>(
            Func<ImmutableArray<TOut>, IEnumerable<T>> shuffleFunc,
            Func<T, TOut> unwrapper)
            => Resequence(shuffleFunc(GetAllInternal(clear: false, unwrapper)));

        public T TakeAt(int index)
            => Break(GetNodeAt(index));

        private Dictionary<int, TOut> GetAllDictionary<TOut>(Func<T, TOut> unwrapper)
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
        private ImmutableArray<TOut> GetAllInternal<TOut>(bool clear, Func<T, TOut> unwrapper)
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
        private void Resequence(IEnumerable<T> newSequence)
        {
            if (newSequence == null)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NewSequenceNull);

            Reset();

            AddSequence(newSequence);
        }
        public void AddSequence(IEnumerable<T> sequence)
        {
            foreach (var item in sequence.Distinct(ReferenceComparer<T>.Instance))
            {
                if (item != null)
                    AddTail(item);
            }
        }

        private void CountIncOne()
            => Interlocked.Increment(ref _count);
        private void CountDecOne()
            => Interlocked.Decrement(ref _count);
        private void Reset()
        {
            Volatile.Write(ref _head, null);
            Volatile.Write(ref _tail, null);
            Volatile.Write(ref _count, 0);
        }

        private Node GetNodeAt(int index)
        {
            if (index == 0)
                return VHead;
            if (index == VCount - 1)
                return VTail;

            var tmp = VHead;
            for (int i = 0; i < index; i++)
                tmp = tmp.Next;

            return tmp;
        }
        internal void AddHead(T item)
        {
            var node = new Node(item);
            var oldHead = Interlocked.Exchange(ref _head, node);
            Interlocked.CompareExchange(ref _tail, value: node, comparand: null);
            CountIncOne();
            node.Next = oldHead;

            if (oldHead != null)
                oldHead.Previous = node;
        }
        private void AddTail(T item)
        {
            var node = new Node(item);
            var oldTail = Interlocked.Exchange(ref _tail, node);
            Interlocked.CompareExchange(ref _head, value: node, comparand: null);
            CountIncOne();
            node.Previous = oldTail;

            if (oldTail != null)
                oldTail.Next = node;
        }
        private void AddAfter(Node node, T item)
        {
            var tmp = new Node(item)
            {
                Next = node?.Next,
                Previous = node
            };

            node.Next = tmp;
            CountIncOne();
        }
        private Node RemHead()
        {
            var headNode = Interlocked.Exchange(ref _head, _head?.Next);
            if (headNode == null)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.PileEmpty);
            Interlocked.CompareExchange(ref _tail, value: null, comparand: headNode);

            if (VHead != null)
                VHead.Previous = null;

            return headNode;
        }
        private Node RemTail()
        {
            var tailNode = Interlocked.Exchange(ref _tail, _tail?.Previous);
            if (tailNode == null)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.PileEmpty);
            Interlocked.CompareExchange(ref _head, value: null, comparand: tailNode);

            if (VHead != null)
                VHead.Previous = null;

            return tailNode;
        }
        private T Break(Node node)
        {
            Interlocked.CompareExchange(ref _head, value: _head?.Next, comparand: node);
            Interlocked.CompareExchange(ref _tail, value: _tail?.Previous, comparand: node);

            if (node.Previous != null)
                node.Previous.Next = node.Next;
            if (node.Next != null)
                node.Next.Previous = node.Previous;

            CountDecOne();

            return node.Value;
        }

        internal ref T GetValueAt(int index)
            => ref GetNodeAt(index).Value;

        private sealed class Node
        {
            private T _value;

            internal Node Next { get; set; }
            internal Node Previous { get; set; }
            internal ref T Value => ref _value;

            internal Node(T value)
            {
                _value = value;
            }
        }

        private static ReadOnlyDictionary<TKey, TValue> Freeze<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values, int cap)
        {
            var d = new Dictionary<TKey, TValue>(capacity: cap);
            foreach (var kvp in values)
                d.Add(kvp.Key, kvp.Value);

            return new ReadOnlyDictionary<TKey, TValue>(d);
        }
    }
}

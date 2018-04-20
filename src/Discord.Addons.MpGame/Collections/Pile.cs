using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame.Collections
{
    /// <summary>
    /// Base type to represent a pile of objects,
    /// specifically for use in card games.
    /// </summary>
    /// <typeparam name="TCard">The card type.</typeparam>
    /// <remarks><div class="markdown level0 remarks"><div class="CAUTION">
    /// <h5>Caution</h5><p>This class is not thread-safe.</p></div></div></remarks>
    [DebuggerDisplay("Count = {Count}")]
    public abstract partial class Pile<TCard>
        where TCard : class
    {
        private static readonly Func<TCard, bool> _defaultPredicate = (_ => true);

        private readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();

        private Node _head;
        private Node _tail;
        private int _count = 0;

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> to an empty state.
        /// </summary>
        protected Pile() { }

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> with the specified cards.
        /// </summary>
        /// <param name="cards">The cards to put in the pile.</param>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>This constructor will filter out any items in
        /// <paramref name="cards"/> that are <see langword="null"/>.</p></div></div></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/>
        /// was <see langword="null"/>.</exception>
        protected Pile(IEnumerable<TCard> cards)
        {
            ThrowArgNull(cards, nameof(cards));

            AddSequence(cards);
        }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> is freely browsable.
        /// </summary>
        public abstract bool CanBrowse { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> can be cleared.
        /// </summary>
        public abstract bool CanClear { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// can be cut, ie. taking a number of cards from the top
        /// and putting them underneath the remainder.
        /// </summary>
        public abstract bool CanCut { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// allows drawing cards from the top.
        /// </summary>
        public abstract bool CanDraw { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// allows drawing cards from the bottom.
        /// </summary>
        public abstract bool CanDrawBottom { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// allows inserting cards at an arbitrary index.
        /// </summary>
        public abstract bool CanInsert { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> allows peeking at cards.
        /// </summary>
        public abstract bool CanPeek { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> allows putting cards on the top.
        /// </summary>
        public abstract bool CanPut { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> allows putting cards on the bottom.
        /// </summary>
        public abstract bool CanPutBottom { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> can be reshuffled.
        /// </summary>
        public abstract bool CanShuffle { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// allows taking cards from an arbitrary index.
        /// </summary>
        public abstract bool CanTake { get; }

        /// <summary>
        /// The amount of cards currently in the pile.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// A snapshot of all the cards
        /// without removing them from the pile.
        /// Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow browsing the cards.</exception>
        public ImmutableArray<TCard> Browse()
        {
            ThrowInvalidOpIf(!CanBrowse, ErrorStrings.NoBrowse);

            using (_rwlock.UsingReadLock())
            {
                return GetAllInternal(clear: false);
            }
        }

        /// <summary>
        /// Clears the entire pile and returns the cards
        /// that were in it. Requires <see cref="CanClear"/>.
        /// </summary>
        /// <returns>The collection as it was before it is cleared.</returns>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow clearing all cards.</exception>
        public ImmutableArray<TCard> Clear()
        {
            ThrowInvalidOpIf(!CanClear, ErrorStrings.NoClear);

            using (_rwlock.UsingWriteLock())
            {
                return GetAllInternal(clear: true);
            }
        }

        /// <summary>
        /// Cuts the pile at a specified number of cards from the top
        /// and places the taken cards on the bottom.
        /// </summary>
        /// <param name="cutIndex">The number of cards to send to the bottom.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cutIndex"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow cutting the pile.</exception>
        public void Cut(int cutAmount)
        {
            ThrowInvalidOpIf(!CanCut, ErrorStrings.NoCut);
            ThrowArgOutOfRangeIf(cutAmount < 0, ErrorStrings.CutIndexNegative, nameof(cutAmount));
            ThrowArgOutOfRangeIf(cutAmount > Count, ErrorStrings.CutIndexTooHigh, nameof(cutAmount));

            if (cutAmount == 0 || cutAmount == Count)
                return; //no changes

            using (_rwlock.UsingWriteLock())
            {
                //cutAmount is 1-indexed, so subtract one here
                var tmp = GetNodeAt(cutAmount - 1);
                _head.Previous = _tail;
                _tail.Next = _head;
                _head = tmp.Next;
                _tail = tmp;
                _head.Previous = null;
                _tail.Next = null;
            }
        }

        /// <summary>
        /// Draws the top card from the pile. If the last card is
        /// drawn, calls <see cref="OnLastRemoved"/>.
        /// Requires <see cref="CanDraw"/>.
        /// </summary>
        /// <returns>If the pile's current size is greater than 0, the card
        /// currently at the top of the pile. Otherwise will throw.</returns>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow drawing cards OR There were no cards to draw.</exception>
        public TCard Draw()
        {
            ThrowInvalidOpIf(!CanDraw, ErrorStrings.NoDraw);
            var topCard = Interlocked.Exchange(ref _head, _head?.Next);
            ThrowInvalidOpIf(topCard == null, ErrorStrings.PileEmpty);

            using (_rwlock.UsingWriteLock())
            {
                var tmp = Break(topCard);

                if (Count == 0)
                    OnLastRemoved();

                return tmp;
            }
        }

        /// <summary>
        /// Draws the bottom card from the pile. If the last card is
        /// drawn, calls <see cref="OnLastRemoved"/>.
        /// Requires <see cref="CanDrawBottom"/>.
        /// </summary>
        /// <returns>If the pile's current size is greater than 0, the card
        /// currently at the bottom of the pile. Otherwise will throw.</returns>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow drawing cards OR There were no cards to draw.</exception>
        public TCard DrawBottom()
        {
            ThrowInvalidOpIf(!CanDraw, ErrorStrings.NoDraw);
            var bottomCard = Interlocked.Exchange(ref _tail, _tail?.Previous);
            ThrowInvalidOpIf(bottomCard == null, ErrorStrings.PileEmpty);

            using (_rwlock.UsingWriteLock())
            {
                var tmp = Break(bottomCard);

                if (Count == 0)
                    OnLastRemoved();

                return tmp;
            }
        }

        /// <summary>
        /// Inserts a card at the given index. Requires <see cref="CanInsert"/>.
        /// </summary>
        /// <param name="card">The card to insert.</param>
        /// <param name="index">The 0-based index to insert at.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow inserting cards at an arbitrary location.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> was <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        public void InsertAt(TCard card, int index)
        {
            ThrowInvalidOpIf(!CanInsert, ErrorStrings.NoInsert);
            ThrowArgNull(card, nameof(card));
            ThrowArgOutOfRangeIf(index < 0, ErrorStrings.InsertionNegative, nameof(index));
            ThrowArgOutOfRangeIf(index > Count, ErrorStrings.InsertionTooHigh, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                if (index == 0)
                    AddHead(card);
                else if (index == Count)
                    AddTail(card);
                else
                    AddAfter(GetNodeAt(index), card);
            }
        }

        /// <summary>
        /// Peeks the <paramref name="amount"/> of
        /// cards without removing them from the pile.
        /// Requires <see cref="CanPeek"/>.
        /// </summary>
        /// <param name="amount">The amount of cards to peek.</param>
        /// <returns>An array of the cards currently at the top of the pile.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow peeking cards.</exception>
        public ImmutableArray<TCard> PeekTop(int amount)
        {
            ThrowInvalidOpIf(!CanPeek, ErrorStrings.NoPeek);
            ThrowArgOutOfRangeIf(amount < 0, ErrorStrings.PeekAmountNegative, nameof(amount));
            ThrowArgOutOfRangeIf(amount > Count, ErrorStrings.PeekAmountTooHigh, nameof(amount));

            if (amount == 0)
                return ImmutableArray<TCard>.Empty;

            using (_rwlock.UsingReadLock())
            {
                var result = new List<TCard>(capacity: amount);

                var tmp = _head;
                for (int i = 0; i < amount; i++)
                    result.Add(tmp.Value);

                return result.ToImmutableArray();
            }
        }

        /// <summary>
        /// Puts a card on top of the pile.
        /// Calls <see cref="OnPut(TCard)"/>.
        /// Requires <see cref="CanPut"/>.
        /// </summary>
        /// <param name="card">The card to place on top.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow placing cards onto it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> was <see langword="null"/>.</exception>
        public void Put(TCard card)
        {
            ThrowInvalidOpIf(!CanPut, ErrorStrings.NoPut);
            ThrowArgNull(card, nameof(card));

            using (_rwlock.UsingWriteLock())
            {
                AddHead(card);
                OnPut(card);
            }
        }

        /// <summary>
        /// Puts a card on the bottom of the pile. Requires <see cref="CanPutBottom"/>.
        /// </summary>
        /// <param name="card">The card to place on the bottom.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow placing cards on the bottom.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> was <see langword="null"/>.</exception>
        public void PutBottom(TCard card)
        {
            ThrowInvalidOpIf(!CanPutBottom, ErrorStrings.NoPutBtm);
            ThrowArgNull(card, nameof(card));

            using (_rwlock.UsingWriteLock())
            {
                AddTail(card);
            }
        }

        /// <summary>
        /// Reshuffles the pile using the specified function.
        /// Requires <see cref="CanShuffle"/>.
        /// </summary>
        /// <param name="shuffleFunc">A function that produces an
        /// <see cref="IEnumerable{TCard}"/> in a new, random order.
        /// This function receives the cards currently in
        /// the pile as its argument.</param>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow reshuffling the cards OR The sequence produced from 
        /// <paramref name="shuffleFunc"/> was <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="shuffleFunc"/> was <see langword="null"/>.</exception>
        public void Shuffle(Func<IEnumerable<TCard>, IEnumerable<TCard>> shuffleFunc)
        {
            ThrowInvalidOpIf(!CanShuffle, ErrorStrings.NoShuffle);
            ThrowArgNull(shuffleFunc, nameof(shuffleFunc));

            using (_rwlock.UsingWriteLock())
            {
                ShuffleInternal(shuffleFunc, GetAllInternal(clear: false));
            }
        }

        /// <summary>
        /// Takes a card from the given index. If the last card is
        /// taken, calls <see cref="OnLastRemoved"/>. Requires <see cref="CanTake"/>.
        /// </summary>
        /// <param name="index">The 0-based index to take.</param>
        /// <returns>The card at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
        /// was less than 0 or greater than or equal to the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow taking cards from an arbitrary location.</exception>
        public TCard TakeAt(int index)
        {
            ThrowInvalidOpIf(!CanTake, ErrorStrings.NoTake);
            ThrowArgOutOfRangeIf(index < 0, ErrorStrings.RetrievalNegative, nameof(index));
            ThrowArgOutOfRangeIf(index >= Count, ErrorStrings.RetrievalTooHighP, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                return TakeInternal(index);
            }
        }

        /// <summary>
        /// Automatically called when the last card is removed from the pile.
        /// </summary>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>Does nothing by default.</p></div></div></remarks>
        protected virtual void OnLastRemoved() { }

        /// <summary>
        /// Automatically called when a card is put on top of the pile.
        /// </summary>
        /// <param name="card">The card that is placed.</param>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>Does nothing by default.</p></div></div></remarks>
        protected virtual void OnPut(TCard card) { }

        /// <summary>
        /// Browse for and take one or more cards from the pile in a single operation.
        /// Requires <see cref="CanBrowse"/> and <see cref="CanTake"/>.
        /// </summary>
        /// <param name="selector">A function that returns an array
        /// of the indeces of the desired cards. The key for each value is the index
        /// of that card. Returning a <see langword="null"/> or empty array
        /// is considered choosing nothing and will return an empty array.</param>
        /// <param name="filter">An optional function to filter
        /// to cards that can be taken.</param>
        /// <param name="shuffleFunc">An optional function to reshuffle the pile
        /// after taking the selected cards. If provided,
        /// requires <see cref="CanShuffle"/>.</param>
        /// <returns>The cards at the chosen indeces, or an empty
        /// array if it was chosen to not take any.</returns>
        /// <exception cref="InvalidOperationException">The pile does not
        /// allow browsing AND taking OR The sequence produced from 
        /// <paramref name="shuffleFunc"/> was <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="selector"/> was <see langword="null"/>.</exception>
        /// <exception cref="IndexOutOfRangeException">One of the
        /// selected indices was not within the provided indices.</exception>
        public async Task<ImmutableArray<TCard>> BrowseAndTake(
            Func<ImmutableDictionary<int, TCard>, Task<int[]>> selector,
            Func<TCard, bool> filter = null,
            Func<IEnumerable<TCard>, IEnumerable<TCard>> shuffleFunc = null)
        {
            ThrowInvalidOpIf(!(CanBrowse && CanTake), ErrorStrings.NoBrowseAndTake);
            ThrowArgNull(selector, nameof(selector));

            using (_rwlock.UsingWriteLock())
            {
                var cards = GetAllD(filter ?? _defaultPredicate);
                var selection = await selector(cards);
                var result = BuildSelection(cards, selection, out var cs);

                if (CanShuffle && shuffleFunc != null)
                    ShuffleInternal(shuffleFunc, cs.Values);

                return result;
            }

            ImmutableArray<TCard> BuildSelection(ImmutableDictionary<int, TCard> cs, int[] sel, out ImmutableDictionary<int, TCard> cs2)
            {
                if (sel == null)
                {
                    cs2 = cs;
                    return ImmutableArray<TCard>.Empty;
                }

                var un = sel.Distinct();
                if (!un.Any())
                {
                    cs2 = cs;
                    return ImmutableArray<TCard>.Empty;
                }

                var ex = un.Except(cs.Keys);
                if (ex.Any())
                    throw new IndexOutOfRangeException($"Selected indeces '{String.Join("', '", ex)}' must be one of the provided card indices.");

                var builder = ImmutableArray.CreateBuilder<TCard>(sel.Length);

                for (int i = 0; i < sel.Length; i++)
                {
                    var s = sel[i];
                    builder.Add(cs[s]);
                    cs = cs.Remove(s);
                }

                cs2 = cs;
                return builder.ToImmutable();
            }
        }

        private ImmutableDictionary<int, TCard> GetAllD(Func<TCard, bool> predicate)
        {
            if (Count == 0)
                return ImmutableDictionary<int, TCard>.Empty;

            var builder = ImmutableDictionary.CreateBuilder<int, TCard>();

            for (var (n, i) = (_head, 0); n != null; (n, i) = (n.Next, i + 1))
            {
                if (predicate(n.Value))
                    builder.Add(i, n.Value);
            }

            return builder.ToImmutable();
        }
        //private ImmutableDictionary<int, TCard> BrowseD()
        //    => BrowseD(_defaultPredicate);
        //private ImmutableDictionary<int, TCard> BrowseD(Func<TCard, bool> predicate)
        //{
        //    ThrowInvalidOpIf(!CanBrowse, ErrorStrings.NoBrowse);
        //    ThrowArgNull(predicate, nameof(predicate));

        //    using (_rwlock.UsingReadLock())
        //    {
        //        return GetAllD(predicate, clear: false);
        //    }
        //}

        private ImmutableArray<TCard> GetAllInternal(bool clear)
        {
            if (Count == 0)
                return ImmutableArray<TCard>.Empty;

            var builder = ImmutableArray.CreateBuilder<TCard>(Count);

            for (var n = _head; n != null; n = n.Next)
                builder.Add(n.Value);

            if (clear)
            {
                _head = null;
                _tail = null;
                Interlocked.Exchange(ref _count, 0);
            }

            return builder.ToImmutable();
        }

        private Node GetNodeAt(int index)
        {
            if (index == 0)
                return _head;
            if (index == Count - 1)
                return _tail;

            var tmp = _head;
            for (int i = 0; i < index; i++)
                tmp = tmp.Next;

            return tmp;
        }
        private Node AddHead(TCard card)
        {
            var tmp = new Node(card);
            var oldHead = Interlocked.Exchange(ref _head, tmp);
            Interlocked.CompareExchange(ref _tail, value: tmp, comparand: null);
            Interlocked.Increment(ref _count);
            tmp.Next = oldHead;

            if (oldHead != null)
                oldHead.Previous = tmp;

            return tmp;
        }
        private Node AddTail(TCard card)
        {
            var tmp = new Node(card);
            var oldTail = Interlocked.Exchange(ref _tail, tmp);
            Interlocked.CompareExchange(ref _head, value: tmp, comparand: null);
            Interlocked.Increment(ref _count);
            tmp.Previous = oldTail;

            if (oldTail != null)
                oldTail.Next = tmp;

            return tmp;
        }
        private Node AddAfter(Node node, TCard card)
        {
            var tmp = new Node(card)
            {
                Next = node?.Next,
                Previous = node
            };

            node.Next = tmp;
            Interlocked.Increment(ref _count);
            return tmp;
        }
        private TCard Break(Node node)
        {
            Interlocked.CompareExchange(ref _head, value: _head?.Next, comparand: node);
            Interlocked.CompareExchange(ref _tail, value: _tail?.Previous, comparand: node);
            if (Interlocked.Decrement(ref _count) <= 0)
                Interlocked.Exchange(ref _count, 0);

            if (node.Previous != null)
                node.Previous.Next = node.Next;
            if (node.Next != null)
                node.Next.Previous = node.Previous;

            return node.Value;
        }
        private TCard TakeInternal(int index)
        {
            var tmp = Break(GetNodeAt(index));

            if (Count == 0)
                OnLastRemoved();

            return tmp;
        }
        private void ShuffleInternal(
            Func<IEnumerable<TCard>, IEnumerable<TCard>> shuffleFunc,
            IEnumerable <TCard> initialPile)
        {
            var shuffled = shuffleFunc(initialPile);

            ThrowInvalidOpIf(shuffled == null, ErrorStrings.NewSequenceNull);

            _head = null;
            _tail = null;
            Interlocked.Exchange(ref _count, 0);

            AddSequence(shuffled);
        }

        //private TCard this[int index]
        //{
        //    get
        //    {
        //        ThrowArgOutOfRangeIf(index < 0, ErrorStrings.RetrievalNegative, nameof(index));
        //        ThrowArgOutOfRangeIf(index >= Count, ErrorStrings.RetrievalTooHighP, nameof(index));

        //        return GetNodeAt(index).Value;
        //    }
        //}

        private void AddSequence(IEnumerable<TCard> cards)
        {
            foreach (var item in cards.Where(c => c != null))
            {
                AddTail(item);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOpIf(bool check, string msg)
        {
            if (check)
                throw new InvalidOperationException(message: msg);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgNull<T>(T arg, string argname)
            where T : class
        {
            if (arg == null)
                throw new ArgumentNullException(paramName: argname);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgOutOfRangeIf(bool check, string msg, string argname)
        {
            if (check)
                throw new ArgumentOutOfRangeException(message: msg, paramName: argname);
        }

        private sealed class Node
        {
            public Node Next     { get; set; }
            public Node Previous { get; set; }
            public TCard Value   { get; }

            public Node(TCard value)
            {
                Value = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
    }
}

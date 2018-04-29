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
    [DebuggerDisplay("Count = {Count}")]
    public abstract partial class Pile<TCard>
        where TCard : class
    {
        private static readonly Func<TCard, bool> _noFilter = (_ => true);

        private readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();

        private Node _head = null;
        private Node _tail = null;
        private int _count = 0;

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> to an empty state.
        /// </summary>
        protected Pile() { }

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> with the specified cards.
        /// </summary>
        /// <param name="cards">The cards to put in the pile.</param>
        /// <remarks><note type="note">This constructor will filter out any items in
        /// <paramref name="cards"/> that are <see langword="null"/>.</note></remarks>
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
        public int Count
        {
            get
            {
                using (_rwlock.UsingReadLock())
                {
                    return _count;
                }
            }
        }

        /// <summary>
        /// Iterates the contents of this pile as an <see cref="IEnumerable{T}"/>.
        /// Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <returns>The contents of the pile in a lazily-evaluated <see cref="IEnumerable{T}"/>.</returns>
        /// <remarks><note type="warning">This method holds a read lock
        /// from when you start enumerating until the enumeration ends
        /// and should be used only for fairly quick one-shot operations.
        /// If you need to hold the data for longer or iterate the same
        /// snapshot more than once, use <see cref="Browse"/> instead.</note></remarks>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow browsing the cards.</exception>
        public IEnumerable<TCard> AsEnumerable()
        {
            if (!CanBrowse)
                ThrowInvalidOp(ErrorStrings.NoBrowse);

            using (_rwlock.UsingReadLock())
            {
                for (var n = _head; n != null; n = n.Next)
                    yield return n.Value;
            }
        }

        /// <summary>
        /// A snapshot of all the cards
        /// without removing them from the pile.
        /// Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow browsing the cards.</exception>
        public ImmutableArray<TCard> Browse()
        {
            if (!CanBrowse)
                ThrowInvalidOp(ErrorStrings.NoBrowse);

            using (_rwlock.UsingReadLock())
            {
                return GetAllInternal(clear: false);
            }
        }

        /// <summary>
        /// Browse for and take one or more cards from the pile in a single operation.
        /// Requires <see cref="CanBrowse"/> and <see cref="CanTake"/>.
        /// </summary>
        /// <param name="selector">A function that returns an array
        /// of the indeces of the desired cards. The key of each pair is the index
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
        /// <exception cref="IndexOutOfRangeException">One or more of the
        /// selected indices was not within the provided indices.</exception>
        /// <example><code language="c#">
        /// // An effect was used that allows the user to
        /// // search their deck for a number of red cards
        /// var picked = await deck.BrowseAndTake(async cards =>
        /// {
        ///     // Example: Call a method that shows
        ///     // the available options to the user
        ///     await ShowUser(cards);
        ///     // Example: Call a method that waits
        ///     // for the user to make a decision, with 'num'
        ///     // being the max amount of cards they can choose
        ///     return await UserInput(num);
        /// },
        /// // Only pass in the red cards
        /// filter: c => c.Color == CardColor.Red,
        /// // Shuffle the pile afterwards
        /// // with some custom function
        /// shuffleFunc: cards => cards.ShuffleItems());
        /// // Add the chosen cards to the user's hand:
        /// foreach (var card in picked)
        ///     player.AddToHand(card);
        /// </code></example>
        public async Task<ImmutableArray<TCard>> BrowseAndTake(
            Func<ImmutableDictionary<int, TCard>, Task<int[]>> selector,
            Func<TCard, bool> filter = null,
            Func<ImmutableArray<TCard>, IEnumerable<TCard>> shuffleFunc = null)
        {
            if (!(CanBrowse && CanTake))
                ThrowInvalidOp(ErrorStrings.NoBrowseAndTake);
            ThrowArgNull(selector, nameof(selector));

            using (_rwlock.UsingWriteLock())
            {
                var cards = GetAllD(filter ?? _noFilter);
                //ReaderWriterLockSlim has thread affinity, so
                //force continuation back onto *this* context.
                var selection = await selector(cards).ConfigureAwait(true);
                var nodes = BuildSelection(selection, ref cards);

                if (CanShuffle && shuffleFunc != null)
                {
                    Resequence(shuffleFunc(cards.Values.ToImmutableArray()));

                    return nodes.Select(n => n.Value).ToImmutableArray();
                }
                else
                {
                    var builder = ImmutableArray.CreateBuilder<TCard>(nodes.Length);
                    foreach (var node in nodes)
                        builder.Add(Break(node));

                    return builder.ToImmutable();
                }
            }

            Node[] BuildSelection(int[] sel, ref ImmutableDictionary<int, TCard> cs)
            {
                if (sel == null)
                    return Array.Empty<Node>();

                var un = sel.Distinct().ToArray();
                if (un.Length == 0)
                    return Array.Empty<Node>();

                var ex = un.Except(cs.Keys);
                if (ex.Any())
                    throw new IndexOutOfRangeException($"Selected indeces '{String.Join(", ", ex)}' must be one of the provided card indices.");

                var arr = new Node[un.Length];

                for (int i = 0; i < un.Length; i++)
                {
                    var s = un[i];
                    arr[i] = GetNodeAt(s);
                    cs = cs.Remove(s);
                }

                return arr;
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
            if (!CanClear)
                ThrowInvalidOp(ErrorStrings.NoClear);

            using (_rwlock.UsingWriteLock())
            {
                return GetAllInternal(clear: true);
            }
        }

        /// <summary>
        /// Cuts the pile at a specified number of cards from the top
        /// and places the taken cards on the bottom.
        /// </summary>
        /// <param name="cutAmount">The number of cards to send to the bottom.</param>
        /// <remarks>If <paramref name="cutAmount"/> is 0 or equal to <see cref="Count"/>,
        /// this function will act like a no-op.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cutAmount"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow cutting the pile.</exception>
        public void Cut(int cutAmount)
        {
            if (!CanCut)
                ThrowInvalidOp(ErrorStrings.NoCut);
            if (cutAmount < 0)
                ThrowArgOutOfRange(ErrorStrings.CutAmountNegative, nameof(cutAmount));
            if (cutAmount > _count)
                ThrowArgOutOfRange(ErrorStrings.CutAmountTooHigh, nameof(cutAmount));

            if (cutAmount == 0 || cutAmount == _count)
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
            if (!CanDraw)
                ThrowInvalidOp(ErrorStrings.NoDraw);

            using (_rwlock.UsingWriteLock())
            {
                var topCard = Interlocked.Exchange(ref _head, _head?.Next);
                if (topCard == null)
                    ThrowInvalidOp(ErrorStrings.PileEmpty);

                var tmp = Break(topCard);

                if (_count == 0)
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
            if (!CanDrawBottom)
                ThrowInvalidOp(ErrorStrings.NoDraw);

            using (_rwlock.UsingWriteLock())
            {
                var bottomCard = Interlocked.Exchange(ref _tail, _tail?.Previous);
                if (bottomCard == null)
                    ThrowInvalidOp(ErrorStrings.PileEmpty);

                var tmp = Break(bottomCard);

                if (_count == 0)
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
            if (!CanInsert)
                ThrowInvalidOp(ErrorStrings.NoInsert);
            ThrowArgNull(card, nameof(card));
            if (index < 0)
                ThrowArgOutOfRange(ErrorStrings.InsertionNegative, nameof(index));
            if (index > _count)
                ThrowArgOutOfRange(ErrorStrings.InsertionTooHigh, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                if (index == 0)
                    AddHead(card);
                else if (index == _count)
                    AddTail(card);
                else
                    AddAfter(GetNodeAt(index), card);
            }
        }

        /// <summary>
        /// Puts the top card of *this* pile on top of another pile.
        /// Requires <see cref="CanDraw"/> on this pile and <see cref="CanPut"/>
        /// on the target pile.
        /// </summary>
        /// <param name="targetPile">The pile to place a card on.</param>
        /// <remarks><note type="note">Calls <see cref="OnPut(TCard)"/> on the target pile.
        /// If the last card of this pile was taken, calls
        /// <see cref="OnLastRemoved"/> *after* the card is placed on the target pile.
        /// </note></remarks>
        /// <exception cref="InvalidOperationException">This pile
        /// does not allow drawing cards OR The target pile
        /// does not allow placing cards on top OR
        /// This pile was empty.</exception>
        public void Mill(Pile<TCard> targetPile)
        {
            if (!CanDraw)
                ThrowInvalidOp(ErrorStrings.NoDraw);
            if (!targetPile.CanPut)
                ThrowInvalidOp(ErrorStrings.NoPutTarget);

            using (_rwlock.UsingWriteLock())
            {
                var topCard = Interlocked.Exchange(ref _head, _head?.Next);
                if (topCard == null)
                    ThrowInvalidOp(ErrorStrings.PileEmpty);

                targetPile.Put(Break(topCard));

                if (_count == 0)
                    OnLastRemoved();
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
            if (!CanPeek)
                ThrowInvalidOp(ErrorStrings.NoPeek);
            if (amount < 0)
                ThrowArgOutOfRange(ErrorStrings.PeekAmountNegative, nameof(amount));
            if (amount > _count)
                ThrowArgOutOfRange(ErrorStrings.PeekAmountTooHigh, nameof(amount));

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
            if (!CanPut)
                ThrowInvalidOp(ErrorStrings.NoPut);
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
            if (!CanPutBottom)
                ThrowInvalidOp(ErrorStrings.NoPutBottom);
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
        public void Shuffle(Func<ImmutableArray<TCard>, IEnumerable<TCard>> shuffleFunc)
        {
            if (!CanShuffle)
                ThrowInvalidOp(ErrorStrings.NoShuffle);
            ThrowArgNull(shuffleFunc, nameof(shuffleFunc));

            using (_rwlock.UsingWriteLock())
            {
                Resequence(shuffleFunc(GetAllInternal(clear: false)));
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
            if (!CanTake)
                ThrowInvalidOp(ErrorStrings.NoTake);
            if (index < 0)
                ThrowArgOutOfRange(ErrorStrings.RetrievalNegative, nameof(index));
            if (index >= _count)
                ThrowArgOutOfRange(ErrorStrings.RetrievalTooHighP, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                return TakeInternal(index);
            }
        }

        /// <summary>
        /// Automatically called when the last card is removed from the pile.
        /// </summary>
        /// <remarks><note type="note">Does nothing by default.</note></remarks>
        protected virtual void OnLastRemoved() { }

        /// <summary>
        /// Automatically called when a card is put on top of the pile.
        /// </summary>
        /// <param name="card">The card that is placed.</param>
        /// <remarks><note type="note">Does nothing by default.</note></remarks>
        protected virtual void OnPut(TCard card) { }

        //private void OnResequence() { }

        private ImmutableDictionary<int, TCard> GetAllD(Func<TCard, bool> predicate)
        {
            if (_count == 0)
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
            if (_count == 0)
                return ImmutableArray<TCard>.Empty;

            var builder = ImmutableArray.CreateBuilder<TCard>(_count);

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
            if (index == _count - 1)
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

            if (_count == 0)
                OnLastRemoved();

            return tmp;
        }
        private void Resequence(IEnumerable<TCard> newSequence)
        {
            if (newSequence == null)
                ThrowInvalidOp(ErrorStrings.NewSequenceNull);

            _head = null;
            _tail = null;
            Interlocked.Exchange(ref _count, 0);

            AddSequence(newSequence);
            //OnResequence();
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

        //private void Mill(Pile<TCard> targetPile)
        //{
        //    var topCard = Interlocked.Exchange(ref _head, _head?.Next);
        //    ThrowInvalidOpIf(topCard == null, ErrorStrings.PileEmpty);

        //    using (_rwlock.UsingWriteLock())
        //    {
        //        targetPile.Put(Break(topCard));
        //    }
        //    if (Count == 0)
        //        OnLastRemoved();
        //}

        private void AddSequence(IEnumerable<TCard> cards)
        {
            foreach (var item in cards.Where(c => c != null))
            {
                AddTail(item);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOp(string msg)
            => throw new InvalidOperationException(message: msg);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgNull<T>(T arg, string argname)
            where T : class
        {
            if (arg == null)
                throw new ArgumentNullException(paramName: argname);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgOutOfRange(string msg, string argname)
            => throw new ArgumentOutOfRangeException(message: msg, paramName: argname);

        private sealed class Node
        {
            public Node Next { get; set; }
            public Node Previous { get; set; }
            public TCard Value { get; }

            public Node(TCard value)
            {
                Value = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
    }
}

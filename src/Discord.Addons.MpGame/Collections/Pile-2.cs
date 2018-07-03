using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame.Collections
{
    /// <summary>
    ///     Base type to represent a pile of objects, specifically for use in card games.
    /// </summary>
    /// <typeparam name="TCard">
    ///     The card type.
    /// </typeparam>
    /// <typeparam name="TWrapper">
    ///     A domain-specific wrapper type, if needed.
    /// </typeparam>
    /// <remarks>
    ///     <note type="caution">
    ///         This version of the type is for advanced usage only.<br/>
    ///         For the simple case, use <see cref="Pile{TCard}"/>.
    ///     </note>
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    public abstract class Pile<TCard, TWrapper>
        where TWrapper : ICardWrapper<TCard>
        where TCard : class
    {
        private static readonly bool _isValueWrapper = typeof(TWrapper).IsValueType;

        private readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();

        private int _count = 0;
        private Node _head = null;
        private Node _tail = null;

        /// <summary>
        ///     Initializes a new pile to an empty state.
        /// </summary>
        protected Pile() { }

        /// <summary>
        ///     Initializes a new pile with the specified cards.
        /// </summary>
        /// <param name="cards">
        ///     The cards to put in the pile.</param>
        /// <remarks>
        ///     <note type="note">
        ///         This constructor will filter out any items in <paramref name="cards"/> that are <see langword="null"/>.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="cards"/> was <see langword="null"/>.
        /// </exception>
        protected Pile(IEnumerable<TCard> cards)
        {
            ThrowHelper.ThrowIfArgNull(cards, nameof(cards));

            AddSequence(cards);
        }

        /// <summary>
        ///     Indicates whether or not this pile is freely browsable.
        /// </summary>
        public abstract bool CanBrowse { get; }

        /// <summary>
        ///     Indicates whether or not this pile can be cleared.
        /// </summary>
        public abstract bool CanClear { get; }

        /// <summary>
        ///     Indicates whether or not this pile can be cut,<br/>
        ///     ie. taking a number of cards from the top and putting them underneath the remainder.
        /// </summary>
        public abstract bool CanCut { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows drawing cards from the top.
        /// </summary>
        public abstract bool CanDraw { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows drawing cards from the bottom.
        /// </summary>
        public abstract bool CanDrawBottom { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows inserting cards at an arbitrary index.
        /// </summary>
        public abstract bool CanInsert { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows peeking at cards.
        /// </summary>
        public abstract bool CanPeek { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows putting cards on the top.
        /// </summary>
        public abstract bool CanPut { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows putting cards on the bottom.
        /// </summary>
        public abstract bool CanPutBottom { get; }

        /// <summary>
        ///     Indicates whether or not this pile can be reshuffled.
        /// </summary>
        public abstract bool CanShuffle { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows taking cards from an arbitrary index.
        /// </summary>
        public abstract bool CanTake { get; }

        /// <summary>
        ///     The amount of cards currently in the pile.
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
        ///     Peeks the single card currently on top without removing it from the pile, or <see langword="null" /> if the pile is empty.<br/>
        ///     Requires <see cref="CanBrowse"/> *or* <see cref="CanPeek"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow browsing *or* peeking the cards.
        /// </exception>
        public TCard Top
        {
            get
            {
                if (!(CanBrowse || CanPeek))
                    ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowseOrPeek);

                using (_rwlock.UsingReadLock())
                {
                    var n = VHead;
                    if (n == null)
                        return null;

                    var wrapper = n.Value;
                    return wrapper.Unwrap(revealing: false);
                }
            }
        }

        private int VCount => Volatile.Read(ref _count);
        private Node VHead => Volatile.Read(ref _head);
        private Node VTail => Volatile.Read(ref _tail);

        /// <summary>
        ///     Iterates the contents of this pile as an <see cref="IEnumerable{T}"/>.<br/>
        ///     Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <returns>
        ///     The contents of the pile in a lazily-evaluated <see cref="IEnumerable{T}"/>.
        /// </returns>
        /// <remarks>
        ///     <note type="warning">
        ///         This method holds a read lock from when you start enumerating until the enumeration ends
        ///         and should be used only for fairly quick one-shot operations (e.g. LINQ).<br/>
        ///         If you need to hold the data for longer or iterate the same
        ///         snapshot more than once, use <see cref="Browse"/> instead.
        ///     </note>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow browsing the cards.
        /// </exception>
        public IEnumerable<TCard> AsEnumerable()
        {
            if (!CanBrowse)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowse);

            return Iterate();

            IEnumerable<TCard> Iterate()
            {
                using (_rwlock.UsingReadLock())
                {
                    for (var n = VHead; n != null; n = n.Next)
                    {
                        var wrapper = n.Value;
                        yield return wrapper.Unwrap(revealing: false);
                    }
                }
            }
        }

        /// <summary>
        ///     A snapshot of all the cards without removing them from the pile.<br/>
        ///     Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow browsing the cards.
        /// </exception>
        public ImmutableArray<TCard> Browse()
        {
            if (!CanBrowse)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowse);

            using (_rwlock.UsingReadLock())
            {
                return GetAllInternal(clear: false);
            }
        }

        /// <summary>
        ///     Browse for and take one or more cards from the pile in a single operation.<br/>
        ///     Requires <see cref="CanBrowse"/> and <see cref="CanTake"/>.
        /// </summary>
        /// <param name="selector">
        ///     A function that returns an array of the indeces of the desired cards.<br/>
        ///     The key of each pair is the index of that card.<br/>
        ///     Returning a <see langword="null"/> or empty array is considered choosing nothing and will return an empty array.
        /// </param>
        /// <param name="filter">
        ///     An optional function to filter to cards that can be taken.
        /// </param>
        /// <param name="shuffleFunc">
        ///     An optional function to reshuffle the pile after taking the selected cards.<br/>
        ///     If provided, requires <see cref="CanShuffle"/>.
        /// </param>
        /// <returns>
        ///     The cards at the chosen indeces, or an empty array if it was chosen to not take any.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The pile does not allow browsing AND taking<br/>
        ///     -OR-<br/>
        ///     The sequence produced from  <paramref name="shuffleFunc"/> was <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="selector"/> was <see langword="null"/>.
        /// </exception>
        /// <exception cref="IndexOutOfRangeException">
        ///     One or more of the selected indices was not within the provided indices.
        /// </exception>
        /// <example>
        ///     <code language="c#">
        ///         // An effect was used that allows the user to
        ///         // search their deck for a number of red cards
        ///         var picked = await deck.BrowseAndTake(async cards =>
        ///         {
        ///             // Example: Call a method that shows
        ///             // the available options to the user
        ///             await ShowUser(cards);
        ///             // Example: Call a method that waits
        ///             // for the user to make a decision, with 'num'
        ///             // being the max amount of cards they can choose
        ///             return await UserInput(num);
        ///         },
        ///         // Only pass in the red cards
        ///         filter: c => c.Color == CardColor.Red,
        ///         // Shuffle the pile afterwards
        ///         // with some custom function
        ///         shuffleFunc: cards => cards.ShuffleItems());
        ///         // Add the chosen cards to the user's hand:
        ///         foreach (var card in picked)
        ///             player.AddToHand(card);
        ///     </code>
        /// </example>
        public async Task<ImmutableArray<TCard>> BrowseAndTake(
            Func<ImmutableDictionary<int, TCard>, Task<int[]>> selector,
            Func<TCard, bool> filter = null,
            Func<ImmutableArray<TCard>, IEnumerable<TCard>> shuffleFunc = null)
        {
            if (!(CanBrowse && CanTake))
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowseAndTake);
            ThrowHelper.ThrowIfArgNull(selector, nameof(selector));

            using (_rwlock.UsingWriteLock())
            {
                var cards = GetAllDictionary();
                var imm = ImmutableDictionary.CreateRange<int, TCard>(
                    (filter != null)
                        ? cards.Where(kv => filter(kv.Value))
                        : cards);

                //ReaderWriterLockSlim has thread affinity, so
                //force continuation back onto *this* context.
                var selection = await selector(imm).ConfigureAwait(true);
                var nodes = BuildSelection(selection, cards, imm);

                if (CanShuffle && shuffleFunc != null)
                {
                    Resequence(shuffleFunc(cards.Values.ToImmutableArray()));

                    return ImmutableArray.CreateRange(nodes.Select(n => n.Value.Unwrap(revealing: true)));
                }
                else
                {
                    return ImmutableArray.CreateRange(nodes.Select(n => Break(n)));
                }
            }

            Node[] BuildSelection(int[] sel, Dictionary<int, TCard> cs, ImmutableDictionary<int, TCard> ics)
            {
                if (sel == null)
                    return Array.Empty<Node>();

                var un = sel.Distinct().ToArray();
                if (un.Length == 0)
                    return Array.Empty<Node>();

                var ex = un.Except(ics.Keys);
                if (ex.Any())
                    ThrowHelper.ThrowIndexOutOfRange($"Selected indeces '{String.Join(", ", ex)}' must be one of the provided card indices.");

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

        /// <summary>
        ///     Clears the entire pile and returns the cards that were in it.<br/>
        ///     Requires <see cref="CanClear"/>.
        /// </summary>
        /// <returns>
        ///     The collection as it was before it is cleared.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow clearing all cards.
        /// </exception>
        public ImmutableArray<TCard> Clear()
        {
            if (!CanClear)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoClear);

            using (_rwlock.UsingWriteLock())
            {
                return GetAllInternal(clear: true);
            }
        }

        /// <summary>
        ///     Cuts the pile at a specified number of cards from the top and places the taken cards on the bottom.<br/>
        ///     Requires <see cref="CanCut"/>.
        /// </summary>
        /// <param name="amount">
        ///     The number of cards to send to the bottom.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         If <paramref name="amount"/> is 0 or equal to <see cref="Count"/>, this function is a no-op.
        ///     </note>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow cutting the pile.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="amount"/> was less than 0 or greater than the pile's current size.
        /// </exception>
        public void Cut(int amount)
        {
            if (!CanCut)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoCut);
            if (amount < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.CutAmountNegative, nameof(amount));
            if (amount > VCount)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.CutAmountTooHigh, nameof(amount));

            if (amount == 0 || amount == VCount)
                return; //no changes

            using (_rwlock.UsingWriteLock())
            {
                var oldHead = VHead;
                var oldTail = VTail;
                var newHead = GetNodeAt(amount);
                var newTail = newHead.Previous;

                oldHead.Previous = oldTail;
                oldTail.Next = oldHead;
                newHead.Previous = null;
                newTail.Next = null;

                //Interlocked.Exchange(ref _head, newHead);
                //Interlocked.Exchange(ref _tail, newTail);
                Volatile.Write(ref _head, newHead);
                Volatile.Write(ref _tail, newTail);
            }
        }

        /// <summary>
        ///     Draws the top card from the pile.<br/>
        ///     If the last card is drawn, calls <see cref="OnLastRemoved"/>.<br/>
        ///     Requires <see cref="CanDraw"/>.
        /// </summary>
        /// <returns>
        /// If the pile's current size is greater than 0, the card currently at the top of the pile. Otherwise will throw.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow drawing cards<br/>
        ///     -OR-<br/>
        ///     There were no cards to draw.
        /// </exception>
        public TCard Draw()
        {
            if (!CanDraw)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoDraw);

            using (_rwlock.UsingWriteLock())
            {
                var topCard = Interlocked.Exchange(ref _head, _head?.Next);
                if (topCard == null)
                    ThrowHelper.ThrowInvalidOp(ErrorStrings.PileEmpty);

                var tmp = Break(topCard);

                if (VCount == 0)
                    OnLastRemoved();

                return tmp;
            }
        }

        /// <summary>
        ///     Draws the bottom card from the pile.<br/>
        ///     If the last card is drawn, calls <see cref="OnLastRemoved"/>.<br/>
        ///     Requires <see cref="CanDrawBottom"/>.
        /// </summary>
        /// <returns>
        ///     If the pile's current size is greater than 0, the card currently at the bottom of the pile. Otherwise will throw.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow drawing cards<br/>
        ///     -OR-<br/>
        ///     There were no cards to draw.
        /// </exception>
        public TCard DrawBottom()
        {
            if (!CanDrawBottom)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoDraw);

            using (_rwlock.UsingWriteLock())
            {
                var bottomCard = Interlocked.Exchange(ref _tail, _tail?.Previous);
                if (bottomCard == null)
                    ThrowHelper.ThrowInvalidOp(ErrorStrings.PileEmpty);

                var tmp = Break(bottomCard);

                if (VCount == 0)
                    OnLastRemoved();

                return tmp;
            }
        }

        /// <summary>
        ///     Inserts a card at the given index.<br/>
        ///     Requires <see cref="CanInsert"/>.
        /// </summary>
        /// <param name="card">
        ///     The card to insert.
        /// </param>
        /// <param name="index">
        ///     The 0-based index to insert at.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow inserting cards at an arbitrary location.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="card"/> was <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than the pile's current size.
        /// </exception>
        public void InsertAt(TCard card, int index)
        {
            if (!CanInsert)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoInsert);
            ThrowHelper.ThrowIfArgNull(card, nameof(card));
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.InsertionNegative, nameof(index));
            if (index > VCount)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.InsertionTooHigh, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                if (index == 0)
                    AddHead(card);
                else if (index == VCount)
                    AddTail(card);
                else
                    AddAfter(GetNodeAt(index), card);
            }
        }


        //public void InsertAt(TCard card, Index index)
        //    => InsertAt(card, index.FromEnd ? (VCount - index.Value) : index.Value);

        /// <summary>
        ///     Puts the top card of *this* pile on top of another pile.<br/>
        ///     Requires <see cref="CanDraw"/> on this pile and <see cref="CanPut"/> on the target pile.
        /// </summary>
        /// <param name="targetPile">
        ///     The pile to place a card on.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         Calls <see cref="OnPut(TCard)"/> on the target pile.<br/>
        ///         If the last card of this pile was taken, calls <see cref="OnLastRemoved"/> as well.
        ///     </note>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     This pile does not allow drawing cards<br/>
        ///     -OR-<br/>
        ///     The target pile does not allow placing cards on top<br/>
        ///     -OR-<br/>
        ///     <paramref name="targetPile"/> was the same instance as the source<br/>
        ///     -OR-<br/>
        ///     This pile was empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="targetPile"/> was <see langword="null"/>.
        /// </exception>
        public void Mill(Pile<TCard, TWrapper> targetPile)
        {
            if (!CanDraw)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoDraw);
            ThrowHelper.ThrowIfArgNull(targetPile, nameof(targetPile));
            if (!targetPile.CanPut)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoPutTarget);
            if (ReferenceEquals(this, targetPile))
                ThrowHelper.ThrowInvalidOp(ErrorStrings.MillTargetSamePile);

            using (_rwlock.UsingWriteLock())
            using (targetPile._rwlock.UsingWriteLock())
            {
                var millNode = Interlocked.Exchange(ref _head, _head?.Next);
                if (millNode == null)
                    ThrowHelper.ThrowInvalidOp(ErrorStrings.PileEmpty);
                Interlocked.CompareExchange(ref _tail, value: _tail?.Previous, comparand: millNode);

                if (VHead != null)
                    VHead.Previous = null;
                CountDecOne();

                var targetHead = Interlocked.Exchange(ref targetPile._head, millNode);
                Interlocked.CompareExchange(ref targetPile._tail, value: millNode, comparand: null);
                millNode.Next = targetHead;

                if (targetHead != null)
                    targetHead.Previous = millNode;
                targetPile.CountIncOne();

                var wrapper = millNode.Value;
                wrapper.Reset(targetPile);

                if (VCount == 0)
                    OnLastRemoved();

                targetPile.OnPut(wrapper.Unwrap(revealing: false));
            }
        }

        /// <summary>
        ///     Peeks the top <paramref name="amount"/> of cards without removing them from the pile.<br/>
        ///     Requires <see cref="CanPeek"/>.
        /// </summary>
        /// <param name="amount">
        ///     The amount of cards to peek.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         Peeking the single top card is done better through <see cref="Top"/>.
        ///     </note>
        /// </remarks>
        /// <returns>
        ///     An array of the cards currently at the top of the pile.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow peeking cards.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="amount"/> was less than 0 or greater than the pile's current size.
        /// </exception>
        public ImmutableArray<TCard> PeekTop(int amount)
        {
            if (!CanPeek)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoPeek);
            if (VCount == 0 || amount == 0)
                return ImmutableArray<TCard>.Empty;
            if (amount < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.PeekAmountNegative, nameof(amount));
            if (amount > VCount)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.PeekAmountTooHigh, nameof(amount));

            using (_rwlock.UsingReadLock())
            {
                var builder = ImmutableArray.CreateBuilder<TCard>(amount);

                for (var (n, i) = (VHead, 0); i < amount; (n, i) = (n.Next, i + 1))
                    builder.Add(n.Value.Unwrap(revealing: CanPeek));

                return builder.ToImmutable();
            }
        }

        /// <summary>
        ///     Puts a card on top of the pile.<br/>
        ///     Calls <see cref="OnPut(TCard)"/>.<br/>
        ///     Requires <see cref="CanPut"/>.
        /// </summary>
        /// <param name="card">
        ///     The card to place on top.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow placing cards onto it.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="card"/> was <see langword="null"/>.
        /// </exception>
        public void Put(TCard card)
        {
            if (!CanPut)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoPut);
            ThrowHelper.ThrowIfArgNull(card, nameof(card));

            using (_rwlock.UsingWriteLock())
            {
                AddHead(card);
                OnPut(card);
            }
        }

        /// <summary>
        ///     Puts a card on the bottom of the pile.<br/>
        ///     Requires <see cref="CanPutBottom"/>.
        /// </summary>
        /// <param name="card">
        ///     The card to place on the bottom.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow placing cards on the bottom.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="card"/> was <see langword="null"/>.
        /// </exception>
        public void PutBottom(TCard card)
        {
            if (!CanPutBottom)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoPutBottom);
            ThrowHelper.ThrowIfArgNull(card, nameof(card));

            using (_rwlock.UsingWriteLock())
            {
                AddTail(card);
            }
        }

        /// <summary>
        ///     Reshuffles the pile using the specified function.<br/>
        ///     Requires <see cref="CanShuffle"/>.
        /// </summary>
        /// <param name="shuffleFunc">
        ///     A function that produces an <see cref="IEnumerable{TCard}"/> in a new, random order.<br/>
        ///     This function receives the cards currently in the pile as its argument.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow reshuffling the cards<br/>
        ///     -OR-<br/>
        ///     The sequence produced from  <paramref name="shuffleFunc"/> was <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="shuffleFunc"/> was <see langword="null"/>.
        /// </exception>
        public void Shuffle(Func<ImmutableArray<TCard>, IEnumerable<TCard>> shuffleFunc)
        {
            if (!CanShuffle)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoShuffle);
            ThrowHelper.ThrowIfArgNull(shuffleFunc, nameof(shuffleFunc));

            using (_rwlock.UsingWriteLock())
            {
                Resequence(shuffleFunc(GetAllInternal(clear: false)));
            }
        }

        /// <summary>
        ///     Takes a card from the given index.<br/>
        ///     If the last card is taken, calls <see cref="OnLastRemoved"/>.<br/>
        ///     Requires <see cref="CanTake"/>.
        /// </summary>
        /// <param name="index">
        ///     The 0-based index to take.
        /// </param>
        /// <returns>
        ///     The card at the specified index.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow taking cards from an arbitrary location.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the pile's current size.
        /// </exception>
        public TCard TakeAt(int index)
        {
            if (!CanTake)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoTake);
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalNegative, nameof(index));
            if (index >= VCount)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalTooHighP, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                return TakeInternal(index);
            }
        }


        //public TCard TakeAt(Index index)
        //    => TakeAt(index.FromEnd ? (VCount - index.Value) : index.Value);

        /// <summary>
        ///     Gets the wrapper object at the specified location.
        /// </summary>
        /// <param name="index">
        ///     The 0-based index to get at.
        /// </param>
        /// <returns>
        ///     The wrapper at the specified index.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the pile's current size.
        /// </exception>
        protected TWrapper GetWrapperAt(int index)
        {
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalNegative, nameof(index));
            if (index >= VCount)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalTooHighP, nameof(index));

            using (_rwlock.UsingReadLock())
            {
                var n = GetNodeAt(index);
                return n.Value;
            }
        }

        /// <summary>
        ///     Gets the wrapper object at the specified location and allows.to update it.<br/>
        ///     This operation is only allowed if <typeparamref name="TWrapper"/> is a value-type (struct).
        /// </summary>
        /// <param name="index">
        ///     The 0-based index to get at.
        /// </param>
        /// <param name="updateFunc">
        ///     A function that performs the updating.<br/>
        ///     Due to the by-value copying, this function should return the updated instance.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The wrapper type for this pile was not a value-type.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the pile's current size.
        /// </exception>
        protected void GetWrapperAndUpdate(int index, Func<TWrapper, TWrapper> updateFunc)
        {
            if (!_isValueWrapper)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.WrapperTypeNotStruct);
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalNegative, nameof(index));
            if (index >= VCount)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalTooHighP, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                var n = GetNodeAt(index);
                var updated = updateFunc(n.Value);
                n.Update(updated);
            }
        }

        /// <summary>
        ///     Automatically called when the last card is removed from the pile.
        /// </summary>
        /// <remarks>
        ///     <note type="note">
        ///         Does nothing by default.
        ///     </note>
        /// </remarks>
        protected virtual void OnLastRemoved() { }

        /// <summary>
        ///     Automatically called when a card is put on top of the pile.
        /// </summary>
        /// <param name="card">
        ///     The card that is placed.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         Does nothing by default.
        ///     </note>
        /// </remarks>
        protected virtual void OnPut(TCard card) { }

        /// <summary>
        ///     Function that puts a <typeparamref name="TCard"/> into a <typeparamref name="TWrapper"/>.
        /// </summary>
        /// <param name="card">
        ///     The card that needs to be wrapped.
        /// </param>
        /// <returns>
        ///     The wrapped version of the card.
        /// </returns>
        protected abstract TWrapper Wrap(TCard card);

        private Dictionary<int, TCard> GetAllDictionary()
        {
            var res = new Dictionary<int, TCard>();
            if (VCount > 0)
            {
                for (var (n, i) = (VHead, 0); n != null; (n, i) = (n.Next, i + 1))
                {
                    var wrapper = n.Value;
                    res.Add(i, wrapper.Unwrap(revealing: CanBrowse));
                }
            }
            return res;
        }
        private ImmutableArray<TCard> GetAllInternal(bool clear)
        {
            if (VCount == 0)
                return ImmutableArray<TCard>.Empty;

            var builder = ImmutableArray.CreateBuilder<TCard>(VCount);

            for (var n = VHead; n != null; n = n.Next)
            {
                var wrapper = n.Value;
                builder.Add(wrapper.Unwrap(revealing: clear));
            }

            if (clear)
            {
                Reset();
            }

            return builder.ToImmutable();
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
        private Node AddHead(TCard card)
        {
            var tmp = new Node(Wrap(card));
            var oldHead = Interlocked.Exchange(ref _head, tmp);
            Interlocked.CompareExchange(ref _tail, value: tmp, comparand: null);
            CountIncOne();
            tmp.Next = oldHead;

            if (oldHead != null)
                oldHead.Previous = tmp;

            return tmp;
        }
        private Node AddTail(TCard card)
        {
            var tmp = new Node(Wrap(card));
            var oldTail = Interlocked.Exchange(ref _tail, tmp);
            Interlocked.CompareExchange(ref _head, value: tmp, comparand: null);
            CountIncOne();
            tmp.Previous = oldTail;

            if (oldTail != null)
                oldTail.Next = tmp;

            return tmp;
        }
        private Node AddAfter(Node node, TCard card)
        {
            var tmp = new Node(Wrap(card))
            {
                Next = node?.Next,
                Previous = node
            };

            node.Next = tmp;
            CountIncOne();
            return tmp;
        }
        private TCard Break(Node node)
        {
            Interlocked.CompareExchange(ref _head, value: _head?.Next, comparand: node);
            Interlocked.CompareExchange(ref _tail, value: _tail?.Previous, comparand: node);
            CountDecOne();

            if (node.Previous != null)
                node.Previous.Next = node.Next;
            if (node.Next != null)
                node.Next.Previous = node.Previous;

            return node.Value.Unwrap(revealing: true);
        }
        private TCard TakeInternal(int index)
        {
            var tmp = Break(GetNodeAt(index));

            if (VCount == 0)
                OnLastRemoved();

            return tmp;
        }
        private void Resequence(IEnumerable<TCard> newSequence)
        {
            if (newSequence == null)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NewSequenceNull);

            Reset();

            AddSequence(newSequence);
        }
        private void AddSequence(IEnumerable<TCard> sequence)
        {
            foreach (var item in sequence)
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
            //Interlocked.Exchange(ref _head, null);
            //Interlocked.Exchange(ref _tail, null);
            //Interlocked.Exchange(ref _count, 0);
            Volatile.Write(ref _head, null);
            Volatile.Write(ref _tail, null);
            Volatile.Write(ref _count, 0);
        }

        private sealed class Node
        {
            internal Node Next { get; set; }
            internal Node Previous { get; set; }
            internal TWrapper Value { get; private set; }

            internal Node(TWrapper value)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                Value = value;
            }

            internal void Update(TWrapper wrapper)
            {
                if (!_isValueWrapper)
                    ThrowHelper.ThrowInvalidOp(ErrorStrings.WrapperTypeNotStruct);

                Value = wrapper;
            }
        }
    }
}

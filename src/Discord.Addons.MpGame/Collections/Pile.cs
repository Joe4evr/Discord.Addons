using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

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
        private Stack<TCard> _top;
        private Queue<TCard> _bottom;
        private bool _strategyUsed = false;
        private IBufferStrategy<TCard> _bufferStrategy = NonPoolingStrategy.Instance;

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> to an empty state.
        /// </summary>
        protected Pile()
        {
            _top = new Stack<TCard>();
            _bottom = new Queue<TCard>();
        }

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

            _top = new Stack<TCard>(cards.Where(c => c != null));
            _bottom = new Queue<TCard>();
        }

        /// <summary>
        /// Defines the strategy used for buffering cards.
        /// </summary>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>The default strategy is to allocate new arrays and
        /// let the GC clean them up.</p></div></div></remarks>
        /// <exception cref="ArgumentNullException">The supplied
        /// value is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The strategy
        /// implementation is already in use.</exception>
        protected IBufferStrategy<TCard> BufferStrategy
        {
            private get => _bufferStrategy;
            set
            {
                ThrowArgNull(value, nameof(value));
                ThrowInvalidOp(_strategyUsed, ErrorStrings.NoSwappingStrategy);

                _bufferStrategy = value;
            }
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
        /// Indicates whether or not this <see cref="Pile{TCard}"/> allows drawing cards.
        /// </summary>
        public abstract bool CanDraw { get; }

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
        public int Count => _top.Count + _bottom.Count;

        /// <summary>
        /// A snapshot of all the cards
        /// without removing them from the pile.
        /// Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow browsing the cards.</exception>
        public IReadOnlyCollection<TCard> Cards
        {
            get
            {
                ThrowInvalidOp(!CanBrowse, ErrorStrings.NoBrowse);

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
        public IReadOnlyCollection<TCard> Clear()
        {
            ThrowInvalidOp(!CanClear, ErrorStrings.NoClear);

            return GetAllInternal(clear: true);
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
        public void Cut(int cutIndex)
        {
            ThrowInvalidOp(!CanCut, ErrorStrings.NoCut);
            ThrowArgOutOfRange(cutIndex < 0, ErrorStrings.CutIndexNegative, nameof(cutIndex));
            ThrowArgOutOfRange(cutIndex > Count, ErrorStrings.CutIndexTooHigh, nameof(cutIndex));

            if (cutIndex == 0 || cutIndex == Count)
                return; //no changes

            for (int i = 0; i < cutIndex; i++)
            {
                _bottom.Enqueue(TakeTopInternal());
            }
        }

        /// <summary>
        /// Draws the top card from the pile. If the last card is
        /// drawn, calls <see cref="OnLastDraw"/>.
        /// Requires <see cref="CanDraw"/>.
        /// </summary>
        /// <returns>If the pile's current size is greater than 0, the card
        /// currently at the top of the pile. Otherwise will throw.</returns>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow drawing cards OR There were no cards to draw.</exception>
        public TCard Draw()
        {
            ThrowInvalidOp(!CanDraw, ErrorStrings.NoDraw);
            ThrowInvalidOp(Count == 0, ErrorStrings.PileEmpty);

            var tmp = TakeTopInternal();

            if (Count == 0)
                OnLastDraw();

            return tmp;
        }

        /// <summary>
        /// Inserts a card at the given index. Requires <see cref="CanInsert"/>.
        /// </summary>
        /// <param name="card">The card to insert.</param>
        /// <param name="index">The index to insert at.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow inserting cards at an arbitrary location.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> was <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        public void InsertAt(TCard card, int index)
        {
            ThrowInvalidOp(!CanInsert, ErrorStrings.NoInsert);
            ThrowArgNull(card, nameof(card));
            ThrowArgOutOfRange(index < 0, ErrorStrings.InsertionNegative, nameof(index));
            ThrowArgOutOfRange(index > Count, ErrorStrings.InsertionTooHigh, nameof(index));

            if (index == 0)
            {
                _top.Push(card);
                return;
            }
            if (index == Count)
            {
                _bottom.Enqueue(card);
                return;
            }

            var buffer = MakeBuffer(index);
            _top.Push(card);
            PushBuffer(buffer, index);
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
        public IReadOnlyCollection<TCard> PeekTop(int amount)
        {
            ThrowInvalidOp(!CanPeek, ErrorStrings.NoPeek);
            ThrowArgOutOfRange(amount < 0, ErrorStrings.PeekAmountNegative, nameof(amount));
            ThrowArgOutOfRange(amount > Count, ErrorStrings.PeekAmountTooHigh, nameof(amount));

            if (amount == 0)
                return ImmutableArray<TCard>.Empty;

            var buffer = MakeBuffer(amount);
            var result = buffer.ToImmutableArray();
            PushBuffer(buffer, amount);
            return result;
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
            ThrowInvalidOp(!CanPut, ErrorStrings.NoPut);
            ThrowArgNull(card, nameof(card));

            _top.Push(card);
            OnPut(card);
        }

        /// <summary>
        /// Puts a card on the bottom of the pile. Requires <see cref="CanPutBottom"/>.
        /// </summary>
        /// <param name="card">The card to place on the bottom.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow inserting cards at an arbitrary location.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> was <see langword="null"/>.</exception>
        public void PutBottom(TCard card)
        {
            ThrowInvalidOp(!CanPutBottom, ErrorStrings.NoPutBtm);
            ThrowArgNull(card, nameof(card));

            _bottom.Enqueue(card);
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
            ThrowInvalidOp(!CanShuffle, ErrorStrings.NoShuffle);
            ThrowArgNull(shuffleFunc, nameof(shuffleFunc));

            var oldCount = Count;
            var shuffled = shuffleFunc(GetAllInternal(clear: false));

            ThrowInvalidOp(shuffled == null, ErrorStrings.NewSequenceNull);

            _top = new Stack<TCard>(shuffled);
        }

        /// <summary>
        /// Takes a card from the given index. Requires <see cref="CanTake"/>.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
        /// was less than 0 or greater than or equal to the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow taking cards from an arbitrary location.</exception>
        public TCard TakeAt(int index)
        {
            ThrowInvalidOp(!CanTake, ErrorStrings.NoTake);
            ThrowArgOutOfRange(index < 0, ErrorStrings.RetrievalNegative, nameof(index));
            ThrowArgOutOfRange(index >= Count, ErrorStrings.RetrievalTooHighP, nameof(index));

            if (index == 0)
                return TakeTopInternal();
            if (index == _top.Count)
                return _bottom.Dequeue();

            var buffer = MakeBuffer(index);
            var tmp = TakeTopInternal();
            PushBuffer(buffer, index);
            return tmp;
        }

        /// <summary>
        /// Automatically called when the last card is drawn.
        /// </summary>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>Does nothing by default.</p></div></div></remarks>
        protected virtual void OnLastDraw() { }

        /// <summary>
        /// Automatically called when a card is put on top of the pile.
        /// </summary>
        /// <param name="card">The card that is placed.</param>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>Does nothing by default.</p></div></div></remarks>
        protected virtual void OnPut(TCard card) { }

        //private bool IsIndexInTop(int i) => i < _top.Count;
        private IReadOnlyCollection<TCard> GetAllInternal(bool clear)
        {
            if (Count == 0)
                return ImmutableArray<TCard>.Empty;

            var size = Count;
            var buffer = MakeBuffer(size);
            var result = ImmutableArray.Create(buffer, 0, size);
            if (!clear)
                PushBuffer(buffer, size);
            else
                BufferStrategy.ReturnBuffer(buffer);

            return result;
        }
        private TCard TakeTopInternal() => (_top.Count > 0) //IsIndexInTop(0)
            ? _top.Pop()
            : _bottom.Dequeue();
        private TCard[] MakeBuffer(int bufferSize)
        {
            if (!_strategyUsed)
                _strategyUsed = true;

            var buffer = BufferStrategy.GetBuffer(bufferSize);
            for (int i = 0; i < bufferSize; i++)
            {
                buffer[i] = TakeTopInternal();
            }
            return buffer;
        }
        private void PushBuffer(TCard[] buffer, int bufferSize)
        {
            for (int i = bufferSize - 1; i >= 0; i--)
            {
                _top.Push(buffer[i]);
            }
            BufferStrategy.ReturnBuffer(buffer);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOp(bool check, string msg)
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
        private static void ThrowArgOutOfRange(bool check, string msg, string argname)
        {
            if (check)
                throw new ArgumentOutOfRangeException(message: msg, paramName: argname);
        }
    }
}

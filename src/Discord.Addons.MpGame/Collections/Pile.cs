using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Discord.Addons.MpGame.Collections
{
    [DebuggerDisplay("Count = {Count}")]
    /// <summary>
    /// Base type to represent a pile of objects,
    /// specifically for use in card games.
    /// </summary>
    /// <typeparam name="TCard">The card type.</typeparam>
    /// <remarks>This class is not thread-safe.</remarks>
    public abstract partial class Pile<TCard>
        where TCard : class
    {
        private Stack<TCard> _top;
        private Queue<TCard> _bottom;

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
        protected Pile(IEnumerable<TCard> cards)
        {
            _top = new Stack<TCard>(cards);
            _bottom = new Queue<TCard>();
        }

        /// <summary>
        /// Defines the strategy used for buffering cards.
        /// </summary>
        /// <remarks>The default strategy is to allocate new arrays and
        /// let the GC clean them up.</remarks>
        internal IBufferStrategy<TCard> BufferStrategy { private get; set; } = NonPoolingStrategy.Instance;

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> is freely browsable.
        /// </summary>
        public abstract bool CanBrowse { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> can be cleared.
        /// </summary>
        public abstract bool CanClear { get; }

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
        /// The cards inside this pile. Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow browsing the cards.</exception>
        public IReadOnlyCollection<TCard> Cards
        {
            get
            {
                ThrowForFailedCheck(CanBrowse, ErrorStrings.NoBrowse);

                return GetAllInternal(clear: false);
            }
        }

        /// <summary>
        /// Draws a card from the pile. If the last card is
        /// drawn, calls <see cref="OnLastDraw"/>.
        /// Requires <see cref="CanDraw"/>.
        /// </summary>
        /// <returns>If the pile's current size is greater than 0, the card
        /// currently at the top of the pile. Otherwise <see langword="null"/>.</returns>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow drawing cards.</exception>
        public TCard Draw()
        {
            ThrowForFailedCheck(CanDraw, ErrorStrings.NoDraw);

            if (Count > 0)
            {
                var tmp = TakeTopInternal();

                if (Count == 0)
                {
                    OnLastDraw();
                }
                return tmp;
            }
            return null;
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
            ThrowForFailedCheck(CanPeek, ErrorStrings.NoPeek);

            if (amount < 0)
                throw new ArgumentOutOfRangeException(message: "Parameter value may not be negative.", paramName: nameof(amount));
            if (amount > Count)
                throw new ArgumentOutOfRangeException(message: "Parameter value may not be greater than the pile's current size.", paramName: nameof(amount));

            var buffer = MakeBuffer(amount);
            PushBuffer(buffer, amount);
            return buffer.ToImmutableArray();
        }

        /// <summary>
        /// Puts a card on top of the pile.
        /// Calls <see cref="OnPut(TCard)"/>.
        /// Requires <see cref="CanPut"/>.
        /// </summary>
        /// <param name="card">The card to place on top.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow placing cards onto it.</exception>
        public void Put(TCard card)
        {
            ThrowForFailedCheck(CanPut, ErrorStrings.NoPut);

            _top.Push(card);
            OnPut(card);
        }

        /// <summary>
        /// Puts a card on the bottom of the pile. Requires <see cref="CanPutBottom"/>.
        /// </summary>
        /// <param name="card">The card to place on the bottom.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow inserting cards at an arbitrary location.</exception>
        public void PutBottom(TCard card)
        {
            ThrowForFailedCheck(CanPutBottom, ErrorStrings.NoPutBtm);

            _bottom.Enqueue(card);
        }

        /// <summary>
        /// Inserts a card at the given index. Requires <see cref="CanInsert"/>.
        /// </summary>
        /// <param name="card">The card to insert.</param>
        /// <param name="index">The index to insert at.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow inserting cards at an arbitrary location.</exception>
        public void InsertAt(TCard card, int index)
        {
            ThrowForFailedCheck(CanInsert, ErrorStrings.NoInsert);

            if (index < 0)
                throw new ArgumentOutOfRangeException(message: "Insertion index may not be negative.", paramName: nameof(index));
            if (index > Count)
                throw new ArgumentOutOfRangeException(message: "Insertion index may not be greater than the pile's current size.", paramName: nameof(index));

            if (index == 0)
            {
                _top.Push(card);
                return;
            }

            var buffer = MakeBuffer(index);
            _top.Push(card);
            PushBuffer(buffer, index);
        }

        /// <summary>
        /// Takes a card from the given index. Requires <see cref="CanTake"/>.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow taking cards from an arbitrary location.</exception>
        public TCard TakeAt(int index)
        {
            ThrowForFailedCheck(CanTake, ErrorStrings.NoTake);

            if (index < 0)
                throw new ArgumentOutOfRangeException(message: "Retrieval index may not be negative.", paramName: nameof(index));
            if (index > Count)
                throw new ArgumentOutOfRangeException(message: "Retrieval index may not be greater than the pile's current size.", paramName: nameof(index));

            if (index == 0)
            {
                return TakeTopInternal();
            }

            var buffer = MakeBuffer(index);
            var tmp = TakeTopInternal();
            PushBuffer(buffer, index);
            return tmp;
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
            ThrowForFailedCheck(CanClear, ErrorStrings.NoClear);

            return GetAllInternal(clear: true);
        }

        /// <summary>
        /// Reshuffles the pile using the specified function.
        /// Requires <see cref="CanShuffle"/>.
        /// </summary>
        /// <param name="shuffleFunc">A function that produces a
        /// new <see cref="IEnumerable{TCard}"/> for the pile.
        /// This function receives the cards currently in
        /// the pile as its argument.</param>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow reshuffling the cards.</exception>
        public void Shuffle(Func<IEnumerable<TCard>, IEnumerable<TCard>> shuffleFunc)
        {
            ThrowForFailedCheck(CanShuffle, ErrorStrings.NoShuffle);

            _top = new Stack<TCard>(shuffleFunc(GetAllInternal(clear: true)));
        }

        /// <summary>
        /// Automatically called when the last card is drawn.
        /// Does nothing by default.
        /// </summary>
        protected virtual void OnLastDraw() { }

        /// <summary>
        /// Automatically called when a card is put on top of the pile.
        /// Does nothing by default.
        /// </summary>
        /// <param name="card">The card that is placed.</param>
        protected virtual void OnPut(TCard card) { }

        private bool IsIndexInTop(int i) => i < _top.Count;
        private IReadOnlyCollection<TCard> GetAllInternal(bool clear)
        {
            var size = Count;
            var buffer = MakeBuffer(size);
            var result = ImmutableArray.Create(buffer, 0, size);
            if (!clear)
                PushBuffer(buffer, size);

            return result;
        }
        private TCard TakeTopInternal() => IsIndexInTop(0)
            ? _top.Pop()
            : _bottom.Dequeue();
        private TCard[] MakeBuffer(int bufferSize)
        {
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
        private static void ThrowForFailedCheck(bool check, string message)
        {
            if (!check)
                throw new InvalidOperationException(message);
        }
    }

    internal static class ErrorStrings
    {
        internal static readonly string NoBrowse  = "Not allowed to browse this instance.";
        internal static readonly string NoClear   = "Not allowed to clear this instance.";
        internal static readonly string NoDraw    = "Not allowed to draw on this instance.";
        internal static readonly string NoInsert  = "Not allowed to insert at arbitrary index on this instance.";
        internal static readonly string NoPeek    = "Not allowed to peek on this instance.";
        internal static readonly string NoPut     = "Not allowed to put cards on top of this instance.";
        internal static readonly string NoPutBtm  = "Not allowed to put cards on the bottom of this instance.";
        internal static readonly string NoShuffle = "Not allowed to reshuffle this instance.";
        internal static readonly string NoTake    = "Not allowed to take from an arbitrary index on this instance.";
    }
}

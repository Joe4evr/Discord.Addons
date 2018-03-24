using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Discord.Addons.MpGame.Collections
{
    /// <summary>
    /// Base type to represent a pile of objects,
    /// specifically for use in card games.
    /// </summary>
    /// <typeparam name="TCard">The card type.</typeparam>
    public abstract class Pile<TCard>
        where TCard : class
    {
        private Stack<TCard> _pile;

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
        public int Count => _pile.Count;

        /// <summary>
        /// The cards inside this pile. Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow browsing the cards.</exception>
        public IReadOnlyCollection<TCard> Cards
        {
            get
            {
                ThrowForFailedCheck(CanBrowse, "Not allowed to browse this instance.");

                return _pile.ToImmutableArray();
            }
        }

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> to an empty state.
        /// </summary>
        protected Pile()
        {
            _pile = new Stack<TCard>();
        }

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> with the specified cards.
        /// </summary>
        /// <param name="cards">The cards to put in the pile.</param>
        protected Pile(IEnumerable<TCard> cards)
        {
            _pile = new Stack<TCard>(cards);
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
            ThrowForFailedCheck(CanDraw, "Not allowed to draw on this instance.");

            if (Count > 0)
            {
                var tmp = _pile.Pop();
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
            ThrowForFailedCheck(CanPeek, "Not allowed to peek on this instance.");

            if (amount < 0)
                throw new ArgumentOutOfRangeException(message: "Parameter value may not be negative.", paramName: nameof(amount));
            if (amount > Count)
                throw new ArgumentOutOfRangeException(message: "Parameter value may not be greater than the pile's current size.", paramName: nameof(amount));

            var buffer = new TCard[amount];
            for (int i = 0; i < amount; i++)
            {
                buffer[i] = _pile.ElementAt(i);
            }
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
            ThrowForFailedCheck(CanPut, "Not allowed to put cards on top of this instance.");

            _pile.Push(card);
            OnPut(card);
        }

        /// <summary>
        /// Puts a card on the bottom of the pile. Requires <see cref="CanInsert"/>.
        /// </summary>
        /// <param name="card">The card to place on the bottom.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow inserting cards at an arbitrary location.</exception>
        public void PutBottom(TCard card)
            => InsertAt(card, Count);

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
            ThrowForFailedCheck(CanInsert, "Not allowed to insert at arbitrary index on this instance.");

            if (index < 0)
                throw new ArgumentOutOfRangeException(message: "Insertion index may not be negative.", paramName: nameof(index));
            if (index > Count)
                throw new ArgumentOutOfRangeException(message: "Insertion index may not be greater than the pile's current size.", paramName: nameof(index));

            if (index == 0)
            {
                _pile.Push(card);
                return;
            }

            var buffer = new TCard[index];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = _pile.Pop();
            }
            _pile.Push(card);
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                _pile.Push(buffer[i]);
            }
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
            ThrowForFailedCheck(CanTake, "Not allowed to take from an arbitrary index on this instance.");

            if (index < 0)
                throw new ArgumentOutOfRangeException(message: "Retrieval index may not be negative.", paramName: nameof(index));
            if (index > Count)
                throw new ArgumentOutOfRangeException(message: "Retrieval index may not be greater than the pile's current size.", paramName: nameof(index));

            if (index == 0)
            {
                return _pile.Pop();
            }

            var buffer = new TCard[index];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = _pile.Pop();
            }
            var tmp = _pile.Pop();
            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                _pile.Push(buffer[i]);
            }
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
            ThrowForFailedCheck(CanClear, "Not allowed to clear this instance.");

            var result = _pile.ToImmutableArray();
            _pile.Clear();
            return result;
        }

        /// <summary>
        /// Reshuffles the pile using the specified function.
        /// Requires <see cref="CanShuffle"/>.
        /// </summary>
        /// <param name="reshuffleFunc">The function that produces a
        /// new <see cref="IEnumerable{TCard}"/> for the pile.</param>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow reshuffling the cards.</exception>
        public void Reshuffle(Func<IEnumerable<TCard>> reshuffleFunc)
        {
            ThrowForFailedCheck(CanShuffle, "Not allowed to reshuffle this instance.");

            _pile = new Stack<TCard>(reshuffleFunc());
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
        protected virtual void OnPut(TCard card) { }

        private static void ThrowForFailedCheck(bool check, string message)
        {
            if (!check)
                throw new InvalidOperationException(message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame.Collections
{
    /// <summary>
    ///     Similar to <see cref="Pile{TCard}"/> but specialized and optimized for representing a hand of cards.
    /// </summary>
    /// <typeparam name="TCard">
    ///     The card type.
    /// </typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class Hand<TCard>
        where TCard : class
    {
        private readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();

        private List<TCard> _hand;

        /// <summary>
        ///     Initializes a new <see cref="Hand{TCard}"/> to an empty state.
        /// </summary>
        public Hand()
        {
            _hand = new List<TCard>();
        }

        /// <summary>
        ///     Initializes a new <see cref="Hand{TCard}"/> with the specified cards.
        /// </summary>
        /// <param name="cards">
        ///     The cards to put in the hand.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         This constructor will filter out any items in <paramref name="cards"/> that are <see langword="null"/>.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="cards"/> was <see langword="null"/>.
        /// </exception>
        public Hand(IEnumerable<TCard> cards)
        {
            if (cards == null)
                ThrowHelper.ThrowArgNull(nameof(cards));

            _hand = new List<TCard>(cards.Where(c => c != null));
        }

        /// <summary>
        ///     The amount of cards currently in the hand.
        /// </summary>
        public int Count => _hand.Count;

        /// <summary>
        ///     Adds a card to the hand.
        /// </summary>
        /// <param name="card">
        ///     The card to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="card"/> was <see langword="null"/>.
        /// </exception>
        public void Add(TCard card)
        {
            if (card == null)
                ThrowHelper.ThrowArgNull(nameof(card));

            using (_rwlock.UsingWriteLock())
            {
                _hand.Add(card);
            }
        }

        public IReadOnlyDictionary<int, TCard> AsIndexed()
        {
            var builder = ImmutableDictionary.CreateBuilder<int, TCard>();
            for (int i = 0; i < _hand.Count; i++)
                builder.Add(i, _hand[i]);

            return builder.ToImmutable();
        }

        /// <summary>
        ///     The cards inside this hand.
        /// </summary>
        public ImmutableArray<TCard> Browse()
        {
            using (_rwlock.UsingReadLock())
            {
                return (Count == 0)
                    ? ImmutableArray<TCard>.Empty
                    : _hand.ToImmutableArray();
            }
        }

        /// <summary>
        ///     Clears the entire hand and returns the cards that were in it.
        /// </summary>
        /// <returns>
        ///     The collection as it was before it is cleared.
        /// </returns>
        public ImmutableArray<TCard> Clear()
        {
            using (_rwlock.UsingWriteLock())
            {
                var result = _hand.ToImmutableArray();
                _hand.Clear();
                return result;
            }
        }

        /// <summary>
        ///     Orders the cards using the specified function.
        /// </summary>
        /// <param name="orderFunc">
        ///     A function that produces an <see cref="IEnumerable{TCard}"/> in a new order.<br/>
        ///     This function receives the cards currently in the hand as its argument.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="orderFunc"/> was <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The sequence produced from <paramref name="orderFunc"/> was <see langword="null"/>.
        /// </exception>
        public void Order(Func<ImmutableArray<TCard>, IEnumerable<TCard>> orderFunc)
        {
            if (orderFunc == null)
                ThrowHelper.ThrowArgNull(nameof(orderFunc));

            using (_rwlock.UsingWriteLock())
            {
                var newOrder = orderFunc(_hand.ToImmutableArray());
                if (newOrder == null)
                    ThrowHelper.ThrowInvalidOp(ErrorStrings.NewSequenceNull);

                _hand = new List<TCard>(newOrder);
            }
        }

        /// <summary>
        ///     Takes a card from the given index.
        /// </summary>
        /// <param name="index">
        ///     The 0-based index of the card to take.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the pile's current size.
        /// </exception>
        public TCard TakeAt(int index)
        {
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalNegative, nameof(index));
            if (index >= Count)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalTooHighH, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                var tmp = _hand[index];
                _hand.RemoveAt(index);
                return tmp;
            }
        }

        /// <summary>
        ///     Takes the first card that matches a given predicate.
        /// </summary>
        /// <param name="predicate">
        ///     The predicate to match.
        /// </param>
        /// <returns>
        ///     The first card to match the given predicate, or <see langword="null"/> if no match found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="predicate"/> was <see langword="null"/>.
        /// </exception>
        public TCard TakeFirstOrDefault(Func<TCard, bool> predicate)
        {
            if (predicate == null)
                ThrowHelper.ThrowArgNull(nameof(predicate));

            using (_rwlock.UsingWriteLock())
            {
                var tmp = _hand.FirstOrDefault(predicate);
                if (tmp != null)
                    _hand.Remove(tmp);

                return tmp;
            }
        }
    }
}

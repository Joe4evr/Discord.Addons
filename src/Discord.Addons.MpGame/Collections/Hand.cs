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
    ///     Similar to <see cref="Pile{T}"/> but specialized and optimized for representing a hand of items.
    /// </summary>
    /// <typeparam name="T">
    ///     The item type.
    /// </typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class Hand<T>
        where T : class
    {
        private readonly ReaderWriterLockSlim _rwlock = new();

        private List<T> _hand;

        /// <summary>
        ///     Initializes a new <see cref="Hand{T}"/> to an empty state.
        /// </summary>
        public Hand()
        {
            _hand = new List<T>();
        }

        /// <summary>
        ///     Initializes a new <see cref="Hand{T}"/> with the specified items.
        /// </summary>
        /// <param name="items">
        ///     The items to put in the hand.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         This constructor will filter out any items in <paramref name="items"/>
        ///         that are <see langword="null"/> or are pointing to the same object instance.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="items"/> was <see langword="null"/>.
        /// </exception>
        public Hand(IEnumerable<T> items)
        {
            if (items is null)
                ThrowHelper.ThrowArgNull(nameof(items));

            _hand = new List<T>(items.Where(c => c != null).Distinct(ReferenceComparer<T>.Instance));
        }

        /// <summary>
        ///     The amount of items currently in the hand.
        /// </summary>
        public int Count => _hand.Count;

        /// <summary>
        ///     Adds an item to the hand.
        /// </summary>
        /// <param name="item">
        ///     The item to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="item"/> was <see langword="null"/>.
        /// </exception>
        public void Add(T item)
        {
            if (item is null)
                ThrowHelper.ThrowArgNull(nameof(item));

            using (_rwlock.UsingWriteLock())
            {
                _hand.Add(item);
            }
        }

        /// <summary>
        ///     Adds multiple items to the hand.
        /// </summary>
        /// <param name="items">
        ///     The items to add.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="items"/> was <see langword="null"/>.
        /// </exception>
        public void AddRange(IEnumerable<T> items)
        {
            if (items is null)
                ThrowHelper.ThrowArgNull(nameof(items));

            using (_rwlock.UsingWriteLock())
            {
                _hand.AddRange(items);
            }
        }

        /// <summary>
        ///     Creates a snapshot if the items inside
        ///     this hand with its respective 0-based index.
        /// </summary>
        public ImmutableArray<(int, T)> AsIndexed()
        {
            using (_rwlock.UsingReadLock())
            {
                var builder = ImmutableArray.CreateBuilder<(int, T)>(_hand.Count);
                for (int i = 0; i < _hand.Count; i++)
                    builder.Add((i, _hand[i]));

                return builder.MoveToImmutable();
            }
        }

        /// <summary>
        ///     The items inside this hand.
        /// </summary>
        public ImmutableArray<T> Browse()
        {
            using (_rwlock.UsingReadLock())
            {
                return (Count == 0)
                    ? ImmutableArray<T>.Empty
                    : _hand.ToImmutableArray();
            }
        }

        /// <summary>
        ///     Clears the entire hand and returns the items that were in it.
        /// </summary>
        /// <returns>
        ///     The collection as it was before it is cleared.
        /// </returns>
        public ImmutableArray<T> Clear()
        {
            using (_rwlock.UsingWriteLock())
            {
                var result = _hand.ToImmutableArray();
                _hand.Clear();
                return result;
            }
        }

        /// <summary>
        ///     Orders the items using the specified function.
        /// </summary>
        /// <param name="orderFunc">
        ///     A function that produces an <see cref="IEnumerable{T}"/> in a new order.<br/>
        ///     This function receives the items currently in the hand as its argument.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="orderFunc"/> was <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     The sequence produced from <paramref name="orderFunc"/> was <see langword="null"/>.
        /// </exception>
        public void Order(Func<ImmutableArray<T>, IEnumerable<T>> orderFunc)
        {
            if (orderFunc is null)
                ThrowHelper.ThrowArgNull(nameof(orderFunc));

            using (_rwlock.UsingWriteLock())
            {
                var newOrder = orderFunc(_hand.ToImmutableArray());
                if (newOrder is null)
                    ThrowHelper.ThrowInvalidOp(PileErrorStrings.NewSequenceNull);

                _hand = new List<T>(newOrder);
            }
        }

        /// <summary>
        ///     Takes a item from the given index.
        /// </summary>
        /// <param name="index">
        ///     The 0-based index of the item to take.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the hand's current size.
        /// </exception>
        public T TakeAt(int index)
        {
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.RetrievalNegative, nameof(index));
            if (index >= Count)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.RetrievalTooHighH, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                var tmp = _hand[index];
                _hand.RemoveAt(index);
                return tmp;
            }
        }

        /// <summary>
        ///     Takes the first item that matches a given predicate.
        /// </summary>
        /// <param name="predicate">
        ///     The predicate to match.
        /// </param>
        /// <returns>
        ///     The first item to match the given predicate, or <see langword="null"/> if no match found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="predicate"/> was <see langword="null"/>.
        /// </exception>
        public T? TakeFirstOrDefault(Func<T, bool> predicate)
        {
            if (predicate is null)
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

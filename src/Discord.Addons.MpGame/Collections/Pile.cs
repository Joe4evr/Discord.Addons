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
    ///     Base type to represent a pile of objects, mostly tailored for use in card games.
    /// </summary>
    /// <typeparam name="T">
    ///     The item type.
    /// </typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public abstract class Pile<T>
        where T : class
    {
        private static readonly Func<T, T> _noOpTransformer = _ => _;
        private readonly ReaderWriterLockSlim _rwlock = new ReaderWriterLockSlim();
        private readonly PileLogic<T> _logic = null!;

        /// <summary>
        ///     Initializes a new pile to an empty state.
        /// </summary>
        protected Pile()
            : this(Enumerable.Empty<T>(), skipLogicInit: false)
        {
        }

        /// <summary>
        ///     Initializes a new pile with the specified items.
        /// </summary>
        /// <param name="items">
        ///     The items to put in the pile.</param>
        /// <remarks>
        ///     <note type="note">
        ///         This constructor will filter out any items in <paramref name="items"/>
        ///         that are <see langword="null"/> or are pointing to the same object instance.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="items"/> was <see langword="null"/>.
        /// </exception>
        protected Pile(IEnumerable<T> items)
            : this(items, skipLogicInit: false)
        {
        }

        private protected Pile(bool skipLogicInit)
            : this(Enumerable.Empty<T>(), skipLogicInit)
        {
        }

        private Pile(IEnumerable<T> items, bool skipLogicInit)
        {
            if (items is null)
                ThrowHelper.ThrowArgNull(nameof(items));

            if (!skipLogicInit)
            {
                _logic = new PileLogic<T>();
                _logic.AddSequence(items);
                Adder = _logic.AddHead;
            }
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
        ///     ie. taking a number of items from the top and putting them underneath the remainder.
        /// </summary>
        public abstract bool CanCut { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows drawing items from the top.
        /// </summary>
        public abstract bool CanDraw { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows drawing items from the bottom.
        /// </summary>
        public abstract bool CanDrawBottom { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows inserting items at an arbitrary index.
        /// </summary>
        public abstract bool CanInsert { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows peeking at items.
        /// </summary>
        public abstract bool CanPeek { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows putting items on the top.
        /// </summary>
        public abstract bool CanPut { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows putting items on the bottom.
        /// </summary>
        public abstract bool CanPutBottom { get; }

        /// <summary>
        ///     Indicates whether or not this pile can be reshuffled.
        /// </summary>
        public abstract bool CanShuffle { get; }

        /// <summary>
        ///     Indicates whether or not this pile allows taking items from an arbitrary index.
        /// </summary>
        public abstract bool CanTake { get; }

        /// <summary>
        ///     The amount of items currently in the pile.
        /// </summary>
        public int Count => GetCount();
        private protected virtual int GetCount() => _logic.VCount;

        internal virtual Action<T> Adder { get; } = null!;

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
        ///     The instance does not allow browsing the items.
        /// </exception>
        public IEnumerable<T> AsEnumerable()
        {
            if (!CanBrowse)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowse);

            return Iterate();

            IEnumerable<T> Iterate()
            {
                using (_rwlock.UsingReadLock())
                {
                    foreach (var item in AsEnumerableCore())
                    {
                        yield return item;
                    }
                }
            }
        }
        private protected virtual IEnumerable<T> AsEnumerableCore()
            => _logic.AsEnumerable(_noOpTransformer);

        /// <summary>
        ///     A snapshot of all the items without removing them from the pile.<br/>
        ///     Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow browsing the items.
        /// </exception>
        public ImmutableArray<T> Browse()
        {
            if (!CanBrowse)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowse);

            using (_rwlock.UsingReadLock())
            {
                return BrowseCore();
            }
        }
        private protected virtual ImmutableArray<T> BrowseCore()
            => _logic.Browse(_noOpTransformer);

        /// <summary>
        ///     Browse for and take one or more items from the pile in a single operation.<br/>
        ///     Requires <see cref="CanBrowse"/> and <see cref="CanTake"/>.
        /// </summary>
        /// <param name="selector">
        ///     A function that returns an array of the indeces of the desired items.<br/>
        ///     The key of each pair is the index of that item.<br/>
        ///     Returning a <see langword="null"/> or empty array is considered choosing nothing and will return an empty array.
        /// </param>
        /// <param name="filter">
        ///     An optional function to filter to items that can be taken.
        /// </param>
        /// <param name="shuffleFunc">
        ///     An optional function to reshuffle the pile after taking the selected items.<br/>
        ///     If provided, requires <see cref="CanShuffle"/>.
        /// </param>
        /// <returns>
        ///     The items at the chosen indeces, or an empty array if it was chosen to not take any.
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
        ///         // search their deck for a number of red items
        ///         var picked = await deck.BrowseAndTake(async items =>
        ///         {
        ///             // Example: Call a method that shows
        ///             // the available options to the user
        ///             await ShowUser(items);
        ///             // Example: Call a method that waits
        ///             // for the user to make a decision, with 'num'
        ///             // being the max amount of items they can choose
        ///             return await UserInput(num);
        ///         },
        ///         // Only pass in the red items
        ///         filter: c => c.Color == itemColor.Red,
        ///         // Shuffle the pile afterwards
        ///         // with some custom function
        ///         shuffleFunc: items => items.ShuffleItems());
        ///         // Add the chosen items to the user's hand:
        ///         foreach (var item in picked)
        ///             player.AddToHand(item);
        ///     </code>
        /// </example>
        public async Task<ImmutableArray<T>> BrowseAndTakeAsync(
            Func<IReadOnlyDictionary<int, T>, Task<int[]>> selector,
            Func<T, bool>? filter = null,
            Func<ImmutableArray<T>, IEnumerable<T>>? shuffleFunc = null)
        {
            if (!(CanBrowse && CanTake))
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowseAndTake);
            if (selector is null)
                ThrowHelper.ThrowArgNull(nameof(selector));

            using (_rwlock.UsingWriteLock())
            {
                //ReaderWriterLockSlim has thread affinity, so
                //force continuation back onto *this* context.
                return await BrowseAndTakeCore(selector, filter, shuffleFunc).ConfigureAwait(true);
            }
        }
        private protected virtual Task<ImmutableArray<T>> BrowseAndTakeCore(
            Func<IReadOnlyDictionary<int, T>, Task<int[]>> selector,
            Func<T, bool>? filter,
            Func<ImmutableArray<T>, IEnumerable<T>>? shuffleFunc)
            => _logic.BrowseAndTakeAsync(selector, filter, shuffleFunc, _noOpTransformer, CanShuffle);

        /// <summary>
        ///     Clears the entire pile and returns the items that were in it.<br/>
        ///     Requires <see cref="CanClear"/>.
        /// </summary>
        /// <returns>
        ///     The collection as it was before it is cleared.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow clearing all items.
        /// </exception>
        public ImmutableArray<T> Clear()
        {
            if (!CanClear)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoClear);

            using (_rwlock.UsingWriteLock())
            {
                return ClearCore();
            }
        }
        private protected virtual ImmutableArray<T> ClearCore()
            => _logic.Clear(_noOpTransformer);

        /// <summary>
        ///     Cuts the pile at a specified number of items from the top and places the taken items on the bottom.<br/>
        ///     Requires <see cref="CanCut"/>.
        /// </summary>
        /// <param name="amount">
        ///     The number of items to send to the bottom.
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
            if (amount > Count)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.CutAmountTooHigh, nameof(amount));

            if (amount == 0 || amount == Count)
                return; //no changes

            using (_rwlock.UsingWriteLock())
            {
                CutCore(amount);
            }
        }
        private protected virtual void CutCore(int amount)
            => _logic.Cut(amount);

        /// <summary>
        ///     Draws the top item from the pile.<br/>
        ///     If the last item is drawn, calls <see cref="OnLastRemoved"/>.<br/>
        ///     Requires <see cref="CanDraw"/>.
        /// </summary>
        /// <returns>
        ///     If the pile's current size is greater than 0, the item currently at the top of the pile. Otherwise will throw.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow drawing items<br/>
        ///     -OR-<br/>
        ///     There were no items to draw.
        /// </exception>
        public T Draw()
        {
            if (!CanDraw)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoDraw);

            using (_rwlock.UsingWriteLock())
            {
                var tmp = DrawCore();

                if (Count == 0)
                    OnLastRemoved();

                return tmp;
            }
        }
        private protected virtual T DrawCore()
            => _logic.Draw();

        /// <summary>
        ///     Draws the bottom item from the pile.<br/>
        ///     If the last item is drawn, calls <see cref="OnLastRemoved"/>.<br/>
        ///     Requires <see cref="CanDrawBottom"/>.
        /// </summary>
        /// <returns>
        ///     If the pile's current size is greater than 0, the item currently at the bottom of the pile. Otherwise will throw.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow drawing items<br/>
        ///     -OR-<br/>
        ///     There were no items to draw.
        /// </exception>
        public T DrawBottom()
        {
            if (!CanDrawBottom)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoDraw);

            using (_rwlock.UsingWriteLock())
            {
                var tmp = DrawBottomCore();

                if (Count == 0)
                    OnLastRemoved();

                return tmp;
            }
        }
        private protected virtual T DrawBottomCore()
            => _logic.DrawBottom();

        /// <summary>
        ///     Inserts an item at the given index.<br/>
        ///     Requires <see cref="CanInsert"/>.
        /// </summary>
        /// <param name="item">
        ///     The item to insert.
        /// </param>
        /// <param name="index">
        ///     The 0-based index to insert at.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow inserting items at an arbitrary location.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="item"/> was <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than the pile's current size.
        /// </exception>
        public void InsertAt(T item, int index)
        {
            if (!CanInsert)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoInsert);
            if (item is null)
                ThrowHelper.ThrowArgNull(nameof(item));
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.InsertionNegative, nameof(index));
            if (index > Count)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.InsertionTooHigh, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                InsertCore(item, index);
            }
        }
        private protected virtual void InsertCore(T item, int index)
            => _logic.InsertAt(item, index);

        //public void InsertAt(T item, Index index)
        //    => InsertAt(item, index.FromEnd ? (VCount - index.Value) : index.Value);

        /// <summary>
        ///     Puts the top item of *this* pile on top of another pile.<br/>
        ///     Requires <see cref="CanDraw"/> or <see cref="CanBrowse"/> on this pile and <see cref="CanPut"/> on the target pile.
        /// </summary>
        /// <param name="targetPile">
        ///     The pile to place a item on.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         Calls <see cref="OnPut(T)"/> on the target pile.<br/>
        ///         If the last item of this pile was taken, calls <see cref="OnLastRemoved"/> as well.
        ///     </note>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     This pile does not allow drawing or browsing items<br/>
        ///     -OR-<br/>
        ///     <paramref name="targetPile"/> was the same instance as the source<br/>
        ///     -OR-<br/>
        ///     <paramref name="targetPile"/> does not allow placing items on top<br/>
        ///     -OR-<br/>
        ///     This pile was empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="targetPile"/> was <see langword="null"/>.
        /// </exception>
        public void Mill(Pile<T> targetPile)
        {
            if (!(CanDraw || CanBrowse))
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoDraw);
            if (targetPile is null)
                ThrowHelper.ThrowArgNull(nameof(targetPile));
            if (ReferenceEquals(this, targetPile))
                ThrowHelper.ThrowInvalidOp(ErrorStrings.MillTargetSamePile);
            if (!targetPile.CanPut)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoPutTarget);

            using (_rwlock.UsingWriteLock())
            using (targetPile._rwlock.UsingWriteLock())
            {
                var milled = MillCore(targetPile);

                if (Count == 0)
                    OnLastRemoved();

                targetPile.OnPut(milled);
            }
        }
        private protected virtual T MillCore(Pile<T> targetPile)
            => _logic.Mill(_noOpTransformer, targetPile.Adder);

        //private ImmutableArray<TItem> MultiMill(Pile<TItem> targetPile, int amount)
        //{

        //}

        /// <summary>
        ///     Peeks a single item at the specified index without removig it from the pile.
        ///     Requires <see cref="CanBrowse"/> or <see cref="CanPeek"/>.
        /// </summary>
        /// <param name="index">
        ///     The 0-based index to peek at.
        /// </param>
        /// <returns>
        ///     The item at the specified index.
        ///     - OR -
        ///     <see langword="null"/> if the pile is empty.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow peeking items.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the pile's current size.
        /// </exception>
        public T? PeekAt(int index)
        {
            if (!(CanPeek || CanBrowse))
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowseOrPeek);
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.PeekAmountNegative, nameof(index));
            if (index > Count)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.PeekAmountTooHigh, nameof(index));
            if (Count == 0)
                return null;

            using (_rwlock.UsingReadLock())
            {
                return PeekAtCore(index);
            }
        }
        private protected virtual T PeekAtCore(int index)
            => _logic.PeekAt(index);

        /// <summary>
        ///     Peeks the top <paramref name="amount"/> of items without removing them from the pile.<br/>
        ///     Requires <see cref="CanBrowse"/> or <see cref="CanPeek"/>.
        /// </summary>
        /// <param name="amount">
        ///     The amount of items to peek.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         Peeking the single top item is done better through <see cref="PeekAt(int)"/>.
        ///     </note>
        /// </remarks>
        /// <returns>
        ///     An array of the items currently at the top of the pile.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow peeking items.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="amount"/> was less than 0 or greater than the pile's current size.
        /// </exception>
        public ImmutableArray<T> PeekTop(int amount)
        {
            if (!(CanPeek || CanBrowse))
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoBrowseOrPeek);
            if (Count == 0 || amount == 0)
                return ImmutableArray<T>.Empty;
            if (amount < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.PeekAmountNegative, nameof(amount));
            if (amount > Count)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.PeekAmountTooHigh, nameof(amount));

            using (_rwlock.UsingReadLock())
            {
                return PeekTopCore(amount);
            }
        }
        private protected virtual ImmutableArray<T> PeekTopCore(int amount)
            => _logic.PeekTop(amount, _noOpTransformer);

        /// <summary>
        ///     Puts an item on top of the pile.<br/>
        ///     Calls <see cref="OnPut(T)"/>.<br/>
        ///     Requires <see cref="CanPut"/>.
        /// </summary>
        /// <param name="item">
        ///     The item to place on top.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow placing items onto it.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="item"/> was <see langword="null"/>.
        /// </exception>
        public void Put(T item)
        {
            if (!CanPut)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoPut);
            if (item is null)
                ThrowHelper.ThrowArgNull(nameof(item));

            using (_rwlock.UsingWriteLock())
            {
                PutCore(item);
                OnPut(item);
            }
        }
        private protected virtual void PutCore(T item)
            => _logic.Put(item);

        /// <summary>
        ///     Puts an item on the bottom of the pile.<br/>
        ///     Requires <see cref="CanPutBottom"/>.
        /// </summary>
        /// <param name="item">
        ///     The item to place on the bottom.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow placing items on the bottom.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="item"/> was <see langword="null"/>.
        /// </exception>
        public void PutBottom(T item)
        {
            if (!CanPutBottom)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoPutBottom);
            if (item is null)
                ThrowHelper.ThrowArgNull(nameof(item));

            using (_rwlock.UsingWriteLock())
            {
                PutBottomCore(item);
            }
        }
        private protected virtual void PutBottomCore(T item)
            => _logic.PutBottom(item);

        /// <summary>
        ///     Reshuffles the pile using the specified function.<br/>
        ///     Requires <see cref="CanShuffle"/>.
        /// </summary>
        /// <param name="shuffleFunc">
        ///     A function that produces an <see cref="IEnumerable{Titem}"/> in a new, random order.<br/>
        ///     This function receives the items currently in the pile as its argument.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow reshuffling the items<br/>
        ///     -OR-<br/>
        ///     The sequence produced from  <paramref name="shuffleFunc"/> was <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="shuffleFunc"/> was <see langword="null"/>.
        /// </exception>
        public void Shuffle(Func<ImmutableArray<T>, IEnumerable<T>> shuffleFunc)
        {
            if (!CanShuffle)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoShuffle);
            if (shuffleFunc is null)
                ThrowHelper.ThrowArgNull(nameof(shuffleFunc));

            using (_rwlock.UsingWriteLock())
            {
                ShuffleCore(shuffleFunc);
            }
        }
        private protected virtual void ShuffleCore(Func<ImmutableArray<T>, IEnumerable<T>> shuffleFunc)
            => _logic.Shuffle(shuffleFunc, _noOpTransformer);

        /// <summary>
        ///     Takes an item from the given index.<br/>
        ///     If the last item is taken, calls <see cref="OnLastRemoved"/>.<br/>
        ///     Requires <see cref="CanTake"/>.
        /// </summary>
        /// <param name="index">
        ///     The 0-based index to take.
        /// </param>
        /// <returns>
        ///     The item at the specified index.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow taking items from an arbitrary location.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the pile's current size.
        /// </exception>
        public T TakeAt(int index)
        {
            if (!CanTake)
                ThrowHelper.ThrowInvalidOp(ErrorStrings.NoTake);
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalNegative, nameof(index));
            if (index >= Count)
                ThrowHelper.ThrowArgOutOfRange(ErrorStrings.RetrievalTooHighP, nameof(index));

            using (_rwlock.UsingWriteLock())
            {
                var tmp = TakeCore(index);

                if (Count == 0)
                    OnLastRemoved();

                return tmp;
            }
        }
        private protected virtual T TakeCore(int index)
            => _logic.TakeAt(index);


        //public T TakeAt(Index index)
        //    => TakeAt(index.FromEnd ? (VCount - index.Value) : index.Value);

        /// <summary>
        ///     Automatically called when the last item is removed from the pile.
        /// </summary>
        /// <remarks>
        ///     <note type="note">
        ///         Does nothing by default.
        ///     </note>
        /// </remarks>
        protected virtual void OnLastRemoved() { }

        /// <summary>
        ///     Automatically called when a item is put on top of the pile.
        /// </summary>
        /// <param name="item">
        ///     The item that is placed.
        /// </param>
        /// <remarks>
        ///     <note type="note">
        ///         Does nothing by default.
        ///     </note>
        /// </remarks>
        protected virtual void OnPut(T item) { }
    }
}

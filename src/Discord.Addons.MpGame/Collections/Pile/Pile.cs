using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Core;
//using Nito.AsyncEx;

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

        private protected readonly AsyncReaderWriterLock _rwlock = new();
        private readonly PileLogic<T, T> _logic = null!;

        /// <summary>
        ///     Initializes a new pile to an empty state.
        /// </summary>
        protected Pile()
            : this(Enumerable.Empty<T>(), skipLogicInit: false, initShuffle: false)
        {
        }

        /// <summary>
        ///     Initializes a new pile with the specified items.
        /// </summary>
        /// <param name="items">
        ///     The items to put in the pile.
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
        protected Pile(IEnumerable<T> items)
            : this(items, skipLogicInit: false, initShuffle: false)
        {
        }

        /// <summary>
        ///     Initializes a new pile with the specified items
        ///     and a flag to determine if those should be shuffled beforehand.
        /// </summary>
        /// <param name="items">
        ///     The items to put in the pile.
        /// </param>
        /// <param name="initShuffle">
        ///     A flag to indicate the items
        ///     should be shuffled by the Pile.
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
        protected Pile(IEnumerable<T> items, bool initShuffle)
            : this(items, skipLogicInit: false, initShuffle: initShuffle)
        {
        }

        private protected Pile(bool skipLogicInit)
            : this(Enumerable.Empty<T>(), skipLogicInit: skipLogicInit, initShuffle: false)
        {
        }

        private Pile(IEnumerable<T> items, bool skipLogicInit, bool initShuffle)
        {
            if (items is null)
                ThrowHelper.ThrowArgNull(nameof(items));

            if (!skipLogicInit)
            {
                _logic = new PileLogic<T, T>(_noOpTransformer, ShuffleItems);
                _logic.AddSequence(items, initShuffle);
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

        internal Action<T> Adder => AddItemCore;
        private protected virtual void AddItemCore(T item) => _logic.AddHead(item);

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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoBrowse);

            return Iterate(this);

            static IEnumerable<T> Iterate(Pile<T> @this)
            {
                using (@this._rwlock.AcquireReadLock())
                {
                    foreach (var item in @this.AsEnumerableCore())
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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoBrowse);

            using (_rwlock.AcquireReadLock())
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
        ///     An asynchronous function that returns an array
        ///     of the indeces of the desired items.<br/>
        ///     The key of each pair is the index of that item.<br/>
        ///     Returning a <see langword="null"/> or empty array is
        ///     considered choosing nothing and will return an empty array.
        /// </param>
        /// <param name="filter">
        ///     An optional function to filter to items that can be taken.
        /// </param>
        /// <param name="shuffle">
        ///     A flag to reshuffle the pile after taking the selected items.<br/>
        ///     If set, will use the implementation provided by <see cref="ShuffleItems"/><br/>
        ///     Will be ignored if <see cref="CanShuffle"/> returns <see langword="false"/>.
        /// </param>
        /// <returns>
        ///     The items at the chosen indeces, or an empty array if it was chosen to not take any.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The pile does not allow browsing AND taking<br/>
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
        ///             {
        ///                 // Example: Call a method that shows
        ///                 // the available options to the user
        ///                 await ShowUser(items);
        ///                 
        ///                 // Example: Call a method that waits
        ///                 // for the user to make a decision, with 'num'
        ///                 // being the max amount of items they can choose
        ///                 return await UserInput(num);
        ///             },
        ///             // Only pass in the red items
        ///             filter: c => c.Color == itemColor.Red,
        ///             // Shuffle the pile afterwards
        ///             shuffle: true);
        ///             
        ///         // Add the chosen items to the user's hand:
        ///         player.AddToHand(picked);
        ///     </code>
        /// </example>
        public async Task<ImmutableArray<T>> BrowseAndTakeAsync(
            Func<IReadOnlyDictionary<int, T>, Task<int[]?>> selector,
            Func<T, bool>? filter = null, bool shuffle = false)
        {
            if (!(CanBrowse && CanTake))
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoBrowseAndTake);
            if (selector is null)
                ThrowHelper.ThrowArgNull(nameof(selector));

            using (_rwlock.AcquireWriteLock())
            {
                return await BrowseAndTakeCore(selector, filter, shuffle).ConfigureAwait(true);
            }
        }
        private protected virtual Task<ImmutableArray<T>> BrowseAndTakeCore(
            Func<IReadOnlyDictionary<int, T>, Task<int[]?>> selector,
            Func<T, bool>? filter, bool shuffle)
            => _logic.BrowseAndTakeAsync(selector, filter, _noOpTransformer, (CanShuffle && shuffle));

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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoClear);

            using (_rwlock.AcquireWriteLock())
            {
                return ClearCore();
            }
        }
        private protected virtual ImmutableArray<T> ClearCore()
            => _logic.Clear(_noOpTransformer);

        /// <summary>
        ///     Cuts the pile at a specified number of items from
        ///     the top and places the taken items on the bottom.<br/>
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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoCut);
            if (amount < 0)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.CutAmountNegative, nameof(amount));
            if (amount > Count)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.CutAmountTooHigh, nameof(amount));

            if (amount == 0 || amount == Count)
                return; //no changes

            using (_rwlock.AcquireWriteLock())
            {
                CutCore(amount);
            }
        }
        /// <inheritdoc cref="Cut(Int32)"/>
        /// <param name="index">
        ///     The index of where to split.
        /// </param>
        public void Cut(Index index)
            => Cut(index.GetOffset(Count));
        private protected virtual void CutCore(int amount)
            => _logic.Cut(amount);

        /// <summary>
        ///     Draws the top item from the pile.<br/>
        ///     If the last item is drawn, calls <see cref="OnLastRemoved"/>.<br/>
        ///     Requires <see cref="CanDraw"/>.
        /// </summary>
        /// <returns>
        ///     If the pile's current size is greater than 0,
        ///     the item currently at the top of the pile.
        ///     Otherwise will throw.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow drawing items<br/>
        ///     -OR-<br/>
        ///     There were no items to draw.
        /// </exception>
        public T Draw()
        {
            if (!CanDraw)
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoDraw);

            using (_rwlock.AcquireWriteLock())
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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoDraw);

            using (_rwlock.AcquireWriteLock())
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
        ///     Draws the top <paramref name="amount"/> of items from the pile.<br/>
        ///     If the last item is drawn, calls <see cref="OnLastRemoved"/>.<br/>
        ///     Requires <see cref="CanDraw"/>.
        /// </summary>
        /// <param name="amount">
        ///     The amount of items to draw.
        /// </param>
        /// <returns>
        ///     If the pile's current size is greater than or equal to <paramref name="amount"/>,
        ///     the first that many items currently at the top of the pile.
        ///     Otherwise will throw.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow drawing items<br/>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="amount"/> was less than 0 or greater than the pile's current size.
        /// </exception>
        public ImmutableArray<T> DrawMultiple(int amount)
        {
            if (!CanDraw)
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoDraw);
            if (amount == 0)
                return ImmutableArray<T>.Empty;
            if (amount < 0)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.RetrievalNegative, nameof(amount));
            if (amount > Count)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.RetrievalTooHighP, nameof(amount));

            using (_rwlock.AcquireWriteLock())
            {
                var tmp = MultiDrawCore(amount);

                if (Count == 0)
                    OnLastRemoved();

                return tmp;
            }
        }

        private protected virtual ImmutableArray<T> MultiDrawCore(int amount)
            => _logic.MultiDraw(amount, _noOpTransformer);

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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoInsert);
            if (item is null)
                ThrowHelper.ThrowArgNull(nameof(item));
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.InsertionNegative, nameof(index));
            if (index > Count)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.InsertionTooHigh, nameof(index));

            using (_rwlock.AcquireWriteLock())
            {
                InsertCore(item, index);
            }
        }
        /// <inheritdoc cref="InsertAt(T, Int32)"/>
        public void InsertAt(T item, Index index)
            => InsertAt(item, index.GetOffset(Count));
        private protected virtual void InsertCore(T item, int index)
            => _logic.InsertAt(item, index);

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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoDraw);
            if (targetPile is null)
                ThrowHelper.ThrowArgNull(nameof(targetPile));
            if (ReferenceEquals(this, targetPile))
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.MillTargetSamePile);
            if (!targetPile.CanPut)
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoPutTarget);

            using (_rwlock.AcquireWriteLock())
            using (targetPile._rwlock.AcquireWriteLock())
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
        ///     The item at the specified index.<br/>
        ///     -OR-<br/>
        ///     <see langword="null"/> if the pile is empty.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow browsing or peeking items.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the pile's current size.
        /// </exception>
        public T? PeekAt(int index)
        {
            if (!(CanPeek || CanBrowse))
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoBrowseOrPeek);
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.PeekAmountNegative, nameof(index));
            if (index > Count)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.PeekAmountTooHigh, nameof(index));
            if (Count == 0)
                return null;

            using (_rwlock.AcquireReadLock())
            {
                return PeekAtCore(index);
            }
        }
        /// <inheritdoc cref="PeekAt(Int32)"/>
        public T? PeekAt(Index index)
            => PeekAt(index.GetOffset(Count));
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
        ///         Peeking the single top item is done better
        ///         through <see cref="PeekAt(Int32)"/>.
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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoBrowseOrPeek);
            if (amount == 0)
                return ImmutableArray<T>.Empty;
            if (amount < 0)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.PeekAmountNegative, nameof(amount));
            if (amount > Count)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.PeekAmountTooHigh, nameof(amount));

            using (_rwlock.AcquireReadLock())
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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoPut);
            if (item is null)
                ThrowHelper.ThrowArgNull(nameof(item));

            using (_rwlock.AcquireWriteLock())
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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoPutBottom);
            if (item is null)
                ThrowHelper.ThrowArgNull(nameof(item));

            using (_rwlock.AcquireWriteLock())
            {
                PutBottomCore(item);
            }
        }
        private protected virtual void PutBottomCore(T item)
            => _logic.PutBottom(item);

        /// <summary>
        ///     Reshuffles the pile using <see cref="ShuffleItems"/>.<br/>
        ///     Requires <see cref="CanShuffle"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The instance does not allow reshuffling the items<br/>
        /// </exception>
        public void Shuffle()
        {
            if (!CanShuffle)
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoShuffle);

            using (_rwlock.AcquireWriteLock())
            {
                ShuffleCore();
            }
        }
        private protected virtual void ShuffleCore()
            => _logic.Shuffle(_noOpTransformer);

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
                ThrowHelper.ThrowInvalidOp(PileErrorStrings.NoTake);
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.RetrievalNegative, nameof(index));
            if (index >= Count)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.RetrievalTooHighP, nameof(index));

            using (_rwlock.AcquireWriteLock())
            {
                var tmp = TakeCore(index);

                if (Count == 0)
                    OnLastRemoved();

                return tmp;
            }
        }
        /// <inheritdoc cref="TakeAt(Int32)"/>
        public T TakeAt(Index index)
            => TakeAt(index.GetOffset(Count));
        private protected virtual T TakeCore(int index)
            => _logic.TakeAt(index);



        /// <summary>
        ///     Default implementation for shuffling this pile's items.
        /// </summary>
        /// <param name="items">
        ///     The items contained in this pile.
        /// </param>
        /// <returns>
        ///     A new sequence of the same items in randomized order.
        /// </returns>
        /// <remarks>
        ///     This implementation is a Fisher-Yates shuffle slightly
        ///     adapted from <a href="https://stackoverflow.com/a/1262619">
        ///     this StackOverflow answer</a>.
        /// </remarks>
        protected virtual IEnumerable<T> ShuffleItems(IEnumerable<T> items)
        {
            var buffer = items.ToArray();
            int n = buffer.Length;
            Span<byte> bytes = new byte[(n / Byte.MaxValue) + 1];

            while (n > 1)
            {
                var box = bytes[..((n / Byte.MaxValue) + 1)];
                int boxSum = 0;
                do
                {
                    RandomNumberGenerator.Fill(box);
                    for (int i = 0; i < box.Length; i++)
                        boxSum += box[i];
                }
                while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));

                int k = (boxSum % n);
                n--;
                var value = buffer[k];
                buffer[k] = buffer[n];
                buffer[n] = value;
            }

            return buffer;
        }
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
        ///     Automatically called when an item is put on top of the pile.
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

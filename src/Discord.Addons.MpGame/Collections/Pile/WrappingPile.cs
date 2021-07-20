using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame.Collections
{
    /// <summary>
    ///     Base type to represent a pile of objects inside a custom wrapper type.
    /// </summary>
    /// <typeparam name="T">
    ///     The item type.
    /// </typeparam>
    /// <typeparam name="TWrapper">
    ///     The wrapper type.
    /// </typeparam>
    /// <remarks>
    ///     <note type="warning">
    ///         This type is for advanced scenarios that require intercepting
    ///         the insertion/removal of items (for example: a single
    ///         pile that has mixed visibility of its contents).<br/>
    ///         Consider inheriting from <see cref="Pile{T}"/> directly
    ///         for the common use-cases.
    ///     </note>
    /// </remarks>
    public abstract class WrappingPile<T, TWrapper> : Pile<T>
        where T : class
        where TWrapper : struct, IWrapper<T>
    {
        private static readonly Func<TWrapper, T> _truthyUnwrapper = w => w.Unwrap(true);
        private static readonly Func<TWrapper, T> _falseyUnwrapper = w => w.Unwrap(false);

        private readonly PileLogic<TWrapper, T> _logic;

        /// <inheritdoc />
        protected WrappingPile()
            : this(Enumerable.Empty<T>(), initShuffle: false)
        {
        }

        /// <inheritdoc />
        protected WrappingPile(IEnumerable<T> items)
            : this(items, initShuffle: false)
        {
        }

        /// <inheritdoc />
        protected WrappingPile(IEnumerable<T> items, bool initShuffle)
            : base(skipLogicInit: true)
        {
            if (items is null)
                ThrowHelper.ThrowArgNull(nameof(items));

            _logic = new(Wrap, ShuffleItems);
            _logic.AddSequence(items, initShuffle);
        }

        /// <summary>
        ///     Puts an instance of type <typeparamref name="T"/>
        ///     into a <typeparamref name="TWrapper"/>.
        /// </summary>
        protected abstract TWrapper Wrap(T item);

        /// <summary>
        ///     Gets a <see langword="ref" /> to a wrapper
        ///     object at the specified index.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="index"/> was less than 0 or greater than or equal to the pile's current size.
        /// </exception>
        protected ref TWrapper GetWrapperRefAt(int index)
        {
            if (index < 0)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.RetrievalNegative, nameof(index));
            if (index >= Count)
                ThrowHelper.ThrowArgOutOfRange(PileErrorStrings.RetrievalTooHighP, nameof(index));

            using (_rwlock.AcquireReadLock())
            {
                return ref _logic.GetValueRefAt(index);
            }
        }

        private protected sealed override int GetCount()
            => _logic.VCount;

        private protected sealed override void AddItemCore(T item)
            => _logic.AddHead(item);

        private protected sealed override IEnumerable<T> AsEnumerableCore()
            => _logic.AsEnumerable(_falseyUnwrapper);
        private protected sealed override ImmutableArray<T> BrowseCore()
            => _logic.Browse(_falseyUnwrapper);
        private protected sealed override Task<ImmutableArray<T>> BrowseAndTakeCore(
            Func<IReadOnlyDictionary<int, T>, Task<int[]?>> selector,
            Func<T, bool>? filter, bool shuffle)
            => _logic.BrowseAndTakeAsync(selector, filter, _truthyUnwrapper, (CanShuffle && shuffle));

        private protected sealed override ImmutableArray<T> ClearCore()
            => _logic.Clear(_truthyUnwrapper);
        private protected sealed override void CutCore(int amount)
            => _logic.Cut(amount);
        private protected sealed override T DrawCore()
            => _logic.Draw().Unwrap(true);
        private protected sealed override T DrawBottomCore()
            => _logic.DrawBottom().Unwrap(true);
        private protected sealed override ImmutableArray<T> MultiDrawCore(int amount)
            => _logic.MultiDraw(amount, _truthyUnwrapper);

        private protected sealed override void InsertCore(T item, int index)
            => _logic.InsertAt(item, index);
        private protected sealed override T MillCore(Pile<T> targetPile)
            => _logic.Mill(_truthyUnwrapper, targetPile.Adder);
        private protected sealed override T PeekAtCore(int index)
            => _logic.PeekAt(index).Unwrap(true);
        private protected sealed override ImmutableArray<T> PeekTopCore(int amount)
            => _logic.PeekTop(amount, _truthyUnwrapper);
        private protected sealed override void PutCore(T item)
            => _logic.Put(item);
        private protected sealed override void PutBottomCore(T item)
            => _logic.PutBottom(item);
        private protected sealed override void ShuffleCore()
            => _logic.Shuffle(_truthyUnwrapper);
        private protected sealed override T TakeCore(int index)
            => _logic.TakeAt(index).Unwrap(true);
    }
}

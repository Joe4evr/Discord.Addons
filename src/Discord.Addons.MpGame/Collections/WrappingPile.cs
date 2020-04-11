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
    public abstract class WrappingPile<T, TWrapper> : Pile<T>
        where T : class
        where TWrapper : struct, IWrapper<T>
    {
        private static readonly Func<TWrapper, T> _truthyUnwrapper = w => w.Unwrap(true);
        private static readonly Func<TWrapper, T> _falseyUnwrapper = w => w.Unwrap(false);
        private readonly PileLogic<TWrapper> _logic = new PileLogic<TWrapper>();
        //private readonly Func<TWrapper, T> _unwrapper;

        /// <inheritdoc />
        protected WrappingPile()
            : this(Enumerable.Empty<T>())
        {
        }

        /// <inheritdoc />
        protected WrappingPile(IEnumerable<T> items)
            : base(skipLogicInit: true)
        {
            if (items is null)
                ThrowHelper.ThrowArgNull(nameof(items));

            //_unwrapper = unwrapper ?? _defunwrapper;
            Adder = i => _logic.AddHead(Wrap(i));

            _logic.AddSequence(items
                .Where(i => i != null)
                .Distinct(ReferenceComparer<T>.Instance)
                .Select(Wrap));
        }

        /// <summary>
        /// 
        /// </summary>
        protected abstract TWrapper Wrap(T item);

        /// <summary>
        /// 
        /// </summary>
        protected ref TWrapper GetWrapperAt(int index)
            => ref _logic.GetValueAt(index);

        private protected sealed override int GetCount()
            => _logic.VCount;

        internal sealed override Action<T> Adder { get; }

        private protected sealed override IEnumerable<T> AsEnumerableCore()
            => _logic.AsEnumerable(_falseyUnwrapper);
        private protected sealed override ImmutableArray<T> BrowseCore()
            => _logic.Browse(_falseyUnwrapper);
        private protected sealed override Task<ImmutableArray<T>> BrowseAndTakeCore(
            Func<IReadOnlyDictionary<int, T>, Task<int[]>> selector,
            Func<T, bool>? filter,
            Func<ImmutableArray<T>, IEnumerable<T>>? shuffleFunc)
            => _logic.BrowseAndTakeAsync(selector, filter, items => shuffleFunc!(items).Select(i => Wrap(i)), _truthyUnwrapper, CanShuffle);
        private protected sealed override ImmutableArray<T> ClearCore()
            => _logic.Clear(_truthyUnwrapper);
        private protected sealed override void CutCore(int amount)
            => _logic.Cut(amount);
        private protected sealed override T DrawCore()
            => _logic.Draw().Unwrap(true);
        private protected sealed override T DrawBottomCore()
            => _logic.DrawBottom().Unwrap(true);
        private protected sealed override void InsertCore(T item, int index)
            => _logic.InsertAt(Wrap(item), index);
        private protected sealed override T MillCore(Pile<T> targetPile)
            => _logic.Mill(_truthyUnwrapper, targetPile.Adder);
        private protected sealed override T PeekAtCore(int index)
            => _logic.PeekAt(index).Unwrap(true);
        private protected sealed override ImmutableArray<T> PeekTopCore(int amount)
            => _logic.PeekTop(amount, _truthyUnwrapper);
        private protected sealed override void PutCore(T item)
            => _logic.Put(Wrap(item));
        private protected sealed override void PutBottomCore(T item)
            => _logic.PutBottom(Wrap(item));
        private protected sealed override void ShuffleCore(Func<ImmutableArray<T>, IEnumerable<T>> shuffleFunc)
            => _logic.Shuffle(items => shuffleFunc(items)?.Select(i => Wrap(i))!, _truthyUnwrapper);
        private protected sealed override T TakeCore(int index)
            => _logic.TakeAt(index).Unwrap(true);
    }
}

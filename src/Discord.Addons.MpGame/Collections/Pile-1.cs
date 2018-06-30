using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Discord.Addons.MpGame.Collections
{
    /// <summary>
    ///     Base type to represent a pile of objects, specifically for use in card games.
    /// </summary>
    /// <typeparam name="TCard">
    ///     The card type.
    /// </typeparam>
    public abstract class Pile<TCard> : Pile<TCard, Pile<TCard>.DefaultWrapper>
        where TCard : class
    {
        /// <inheritdoc/>
        protected Pile() : base() { }
        /// <inheritdoc/>
        protected Pile(IEnumerable<TCard> cards) : base(cards) { }

        /// <inheritdoc/>
        protected sealed override DefaultWrapper Wrap(TCard card)
            => new DefaultWrapper(card);

        /// <summary>
        ///     Default, lightwewight wrapper.
        /// </summary>
        public struct DefaultWrapper : ICardWrapper<TCard>
        {
            private readonly TCard _card;

            internal DefaultWrapper(TCard card)
            {
                _card = card;
            }

            TCard ICardWrapper<TCard>.Unwrap()
            {
                ThrowIfDefault();
                return _card;
            }
            void ICardWrapper<TCard>.Reset<TWrapper>(Pile<TCard, TWrapper> newPile)
                => ThrowIfDefault();

            [MethodImpl(MethodImplOptions.NoInlining), DebuggerStepThrough]
            private void ThrowIfDefault()
            {
                if (_card == null)
                    throw new NullReferenceException();
            }
        }
    }
}

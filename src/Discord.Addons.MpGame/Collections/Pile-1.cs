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
        /// <summary>
        ///     Initializes a new pile to an empty state.
        /// </summary>
        protected Pile() : base() { }

        /// <summary>
        ///     Initializes a new pile with the specified cards.
        /// </summary>
        /// <param name="cards">
        ///     The cards to put in the pile.</param>
        /// <remarks>
        ///     <note type="note">
        ///         This constructor will filter out any items in <paramref name="cards"/> that are <see langword="null"/>.
        ///     </note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="cards"/> was <see langword="null"/>.
        /// </exception>
        protected Pile(IEnumerable<TCard> cards) : base(cards) { }

        /// <inheritdoc/>
        protected sealed override DefaultWrapper Wrap(TCard card)
            => new DefaultWrapper(card);

        /// <summary>
        ///     Default, lightweight wrapper.
        /// </summary>
        public struct DefaultWrapper : ICardWrapper<TCard>
        {
            private readonly TCard _card;

            internal DefaultWrapper(TCard card)
            {
                if (card == null)
                    throw new ArgumentNullException(nameof(card));

                _card = card;
            }

            /// <inheritdoc/>
            public TCard Unwrap(bool _)
            {
                ThrowIfDefault();
                return _card;
            }

            /// <inheritdoc/>
            public void Reset<TWrapper>(Pile<TCard, TWrapper> _)
                where TWrapper : ICardWrapper<TCard>
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

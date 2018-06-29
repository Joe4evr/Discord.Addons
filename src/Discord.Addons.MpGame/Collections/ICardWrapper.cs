using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame.Collections
{
    public interface ICardWrapper<TCard>
        where TCard : class
    {
        /// <summary>
        /// Unwraps the wrapped card.
        /// </summary>
        /// <returns>The unwrapped <typeparamref name="TCard"/>.</returns>
        TCard Unwrap();

        /// <summary>
        /// Called when ownership of the wrapper has
        /// transfered from one pile to a different pile.
        /// </summary>
        /// <param name="newPile">The new Pile that
        /// now owns this wrapper.</param>
        void Reset<TWrapper>(Pile<TCard, TWrapper> newPile)
            where TWrapper : ICardWrapper<TCard>;
    }
}

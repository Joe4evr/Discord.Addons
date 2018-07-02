namespace Discord.Addons.MpGame.Collections
{
    public interface ICardWrapper<TCard>
        where TCard : class
    {
        /// <summary>
        ///     Unwraps the wrapped card.
        /// </summary>
        /// <param name="revealing">
        ///     Indicates if the actual card is required to be returned.
        /// </param>
        /// <returns>
        ///     The unwrapped <typeparamref name="TCard"/>.
        /// </returns>
        /// <remarks>
        ///     <note type="implement">
        ///         If <paramref name="revealing"/> is <see langword="true"/>, any domain-specific checks are required to be skipped and the actual wrapped card must be returned.
        ///     </note>
        /// </remarks>
        TCard Unwrap(bool revealing);

        /// <summary>
        ///     Called when ownership of the wrapper has transfered from one pile to a different pile.
        /// </summary>
        /// <param name="newPile">
        ///     The new Pile that now owns this wrapper.
        /// </param>
        void Reset<TWrapper>(Pile<TCard, TWrapper> newPile)
            where TWrapper : ICardWrapper<TCard>;
    }
}

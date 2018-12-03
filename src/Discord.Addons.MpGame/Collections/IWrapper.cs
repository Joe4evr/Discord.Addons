namespace Discord.Addons.MpGame.Collections
{
    public interface IWrapper<out T>
    {
        /// <summary>
        ///     Unwraps the wrapped item.<br/>
        ///     <i>May</i> do domain-specific checks or operations.
        /// </summary>
        /// <param name="revealing">
        ///     Indicates if the actual card is required to be returned.
        /// </param>
        /// <returns>
        ///     The unwrapped item, or a domain-specific placeholder item.
        /// </returns>
        /// <remarks>
        ///     <note type="implement">
        ///         If <paramref name="revealing"/> is <see langword="true"/>,
        ///         any domain-specific checks are required to be skipped
        ///         and the actual wrapped item must be returned.
        ///     </note>
        /// </remarks>
        T Unwrap(bool revealing);
    }
}

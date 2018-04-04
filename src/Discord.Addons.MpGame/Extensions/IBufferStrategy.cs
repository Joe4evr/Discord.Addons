namespace Discord.Addons
{
    /// <summary>
    /// Defines a strategy to use for managing temporary buffers.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    internal interface IBufferStrategy<T>
    {
        /// <summary>
        /// Gets a buffer of the specified size.
        /// </summary>
        /// <param name="size">The minimum wanted size.</param>
        /// <returns>An arry that has at least <paramref name="size"/></returns>
        T[] GetBuffer(int size);

        /// <summary>
        /// Returns the buffer if it was rented from a pool.
        /// </summary>
        /// <param name="buffer">The buffer to return.</param>
        void ReturnBuffer(T[] buffer);
    }
}
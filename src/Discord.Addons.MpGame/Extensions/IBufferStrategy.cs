namespace Discord.Addons
{
    /// <summary>
    /// Defines a strategy to use for managing temporary buffers.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    public interface IBufferStrategy<T>
    {
        //public static IBufferStrategy<T> NonPoolingStrategy
        //{
        //    get
        //    {
        //        if (_nonPooling == null)
        //            Interlocked.CompareExchange(ref _nonPooling, new NonPooling(), null);
        //        return _nonPooling;
        //    }
        //}
        //private static IBufferStrategy<T> _nonPooling;

        /// <summary>
        /// Gets a buffer of the specified size.
        /// </summary>
        /// <param name="size">The minimum wanted size.</param>
        /// <returns>An arry that is at least <paramref name="size"/> elements long.</returns>
        T[] GetBuffer(int size);

        /// <summary>
        /// Returns the buffer if it was rented from a pool.
        /// </summary>
        /// <param name="buffer">The buffer to return.</param>
        void ReturnBuffer(T[] buffer);

        //private sealed class NonPooling : IBufferStrategy<T>
        //{
        //    T[] IBufferStrategy<T>.GetBuffer(int size) => new T[size];
        //    void IBufferStrategy<T>.ReturnBuffer(T[] buffer) { } //explicit no-op
        //}
    }
}

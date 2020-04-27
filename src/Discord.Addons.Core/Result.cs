//using System;
//using System.Diagnostics.CodeAnalysis;
//using System.Runtime.InteropServices;

//namespace Discord.Addons.Core
//{
//    [StructLayout(LayoutKind.Auto)]
//    /// <summary>
//    ///     Represents the result of an operation that can succeed or fail,
//    ///     but does not produce a value.
//    /// </summary>
//    internal readonly struct Result
//    {
//        /// <summary>
//        ///     Indicates whether the operation was successful.
//        /// </summary>
//        public bool IsSuccess => _isInited && _isSuccess;

//        /// <summary>
//        ///     Message that communicates additional information about the operation.
//        /// </summary>
//        public string? Message => _isInited ? _message : throw new InvalidOperationException("Result instance is uninitialized.");

//        private readonly bool _isInited;
//        private readonly bool _isSuccess;
//        private readonly string? _message;
//        private readonly Exception? _exception;

//        internal Result(bool success, string? message, Exception? exception)
//        {
//            _isInited = true;
//            _isSuccess = success;
//            _message = message;
//            _exception = exception;
//        }

//        /// <summary>
//        ///     Copies this <see cref="Result"/> instance to a <see cref="Result{T}"/>
//        ///     specified by <typeparamref name="T"/>.
//        /// </summary>
//        public Result<T> Of<T>([AllowNull] T value = default)
//            => _isInited
//                ? new Result<T>(value, _isSuccess, _message, _exception)
//                : default;

//        /// <summary>
//        ///     Indicates if this result contains a wrapped exception.
//        /// </summary>
//        /// <param name="exception">
//        ///     The exception, if wrapped. Otherwise <see langword="null"/>.
//        /// </param>
//        /// <returns>
//        ///     <see langword="true"/> if an exception was wrapped.
//        ///     Otherwise <see langword="false"/>.
//        /// </returns>
//        public bool HasException([NotNullWhen(true)] out Exception? exception)
//        {
//            exception = (_isInited && (_exception is { } ex)) ? ex : null;
//            return !(exception is null);
//        }

//        /// <summary>
//        ///     Creates a <see cref="Result"/> of a failed operation.
//        /// </summary>
//        /// <param name="message">
//        ///     Message describing the failure.
//        /// </param>
//        /// <exception cref="ArgumentNullException">
//        ///     <paramref name="message"/> was <see langword="null"/> or entirely whitespace.
//        /// </exception>
//        public static Result Fault(string message)
//        {
//            if (String.IsNullOrWhiteSpace(message))
//                throw new ArgumentNullException(nameof(message),
//                    message: "Must provide a non-empty fault message.");

//            return new Result(false, message, exception: null);
//        }

//        /// <summary>
//        ///     Creates a <see cref="Result"/> of a failed operation
//        ///     with a caught exception.
//        /// </summary>
//        /// <param name="exception">
//        ///     The exception that occured.
//        /// </param>
//        /// <param name="message">
//        ///     Message describing the failure. Optional.
//        /// </param>
//        /// <exception cref="ArgumentNullException">
//        ///     <paramref name="exception"/> was <see langword="null"/>.
//        /// </exception>
//        public static Result Fault(Exception exception, string? message = null)
//        {
//            if (exception is null)
//                throw new ArgumentNullException(nameof(exception));

//            return new Result(false, message, exception);
//        }

//        /// <summary>
//        ///     Creates a <see cref="Result"/> of a successful operation.
//        /// </summary>
//        /// <param name="message">
//        ///     Additional info message, optional.
//        /// </param>
//        public static Result Success(string? message = null)
//            => new Result(true, message, exception: null);

//        //

//        /// <summary>
//        ///     Creates a <see cref="Result{T}"/> of a successful operation.
//        /// </summary>
//        /// <param name="value">
//        ///     The result object.
//        /// </param>
//        /// <param name="message">
//        ///     Additional info message, optional.
//        /// </param>
//        public static Result<T> Success<T>(T value, string? message = null)
//            => Result<T>.Success(value, message);

//        ///// <summary>
//        /////     Creates a <see cref="Result{T}"/> of a failed operation.
//        ///// </summary>
//        ///// <param name="message">
//        /////     Message describing the failure.
//        ///// </param>
//        ///// <exception cref="ArgumentNullException">
//        /////     <paramref name="message"/> was <see langword="null"/> or entirely whitespace.
//        ///// </exception>
//        //public static Result<T> Fault<T>(string message, T _)
//        //    => Result<T>.Fault(message);
//    }

//    [StructLayout(LayoutKind.Auto)]
//    /// <summary>
//    ///     Represents the result of an operation that can produce a value or fail.
//    /// </summary>
//    /// <typeparam name="T">
//    ///     Type of the result object, if successful.
//    /// </typeparam>
//    internal readonly struct Result<T>
//    {
//        /// <summary>
//        ///     Message that communicates additional information about the operation.
//        /// </summary>
//        public string? Message => _isInited ? _message : throw new InvalidOperationException("Result instance is uninitialized.");

//        [AllowNull, MaybeNull]
//        private readonly T _value;

//        private readonly bool _isInited;
//        private readonly bool _isSuccess;
//        private readonly string? _message;
//        private readonly Exception? _exception;

//        internal Result([AllowNull] T value, bool success, string? message, Exception? exception)
//        {
//            _isInited = true;
//            _isSuccess = success;
//            _value = value;
//            _message = message;
//            _exception = exception;
//        }

//        /// <summary>
//        ///     Copies this <see cref="Result{T}"/> into a new <see cref="Result{T}"/>
//        ///     specified by <typeparamref name="U"/>
//        /// </summary>
//        /// <typeparam name="U">
//        ///     The new type argument.
//        /// </typeparam>
//        /// <remarks>
//        ///     If the current instance is uninitialized, this also returns an uninitialized instance.
//        ///     If the current instance is a Success, but its value was not convertable to
//        ///     <typeparamref name="U"/>, this returns a Failed result.
//        /// </remarks>
//        public Result<U> As<U>()
//        {
//            if (!_isInited)
//                return default;

//            if (!_isSuccess || _value is null)
//                return new Result<U>(default, _isSuccess, _message, _exception);

//            return (_value is U val)
//                ? new Result<U>(val, _isSuccess, _message, _exception)
//                : Result<U>.Fault($"Could not cast type '{typeof(T)}' as '{typeof(U)}'.");
//        }

//        /// <summary>
//        ///     Indicates if this result contains a wrapped exception.
//        /// </summary>
//        /// <param name="exception">
//        ///     The exception, if wrapped. Otherwise <see langword="null"/>.
//        /// </param>
//        /// <returns>
//        ///     <see langword="true"/> if an exception was wrapped.
//        ///     Otherwise <see langword="false"/>.
//        /// </returns>
//        public bool HasException([NotNullWhen(true)] out Exception? exception)
//        {
//            exception = (_isInited && (_exception is { } ex)) ? ex : null;
//            return !(exception is null);
//        }

//        /// <summary>
//        ///     Indicates whether the operation was successful.
//        /// </summary>
//        /// <param name="value">
//        ///     If the operation was successful, will contain the result value.
//        ///     Otherwise, it will be the default value of <typeparamref name="T"/>.
//        /// </param>
//        public bool IsSuccess(out T value)
//        {
//            var success = (_isInited && _isSuccess);
//            value = success ? _value : default!;
//            return success;
//        }

//        /// <summary>
//        ///     Creates a <see cref="Result{T}"/> of a failed operation.
//        /// </summary>
//        /// <param name="message">
//        ///     Message describing the failure.
//        /// </param>
//        /// <exception cref="ArgumentNullException">
//        ///     <paramref name="message"/> was <see langword="null"/> or entirely whitespace.
//        /// </exception>
//        public static Result<T> Fault(string message)
//        {
//            if (String.IsNullOrWhiteSpace(message))
//                throw new ArgumentNullException(nameof(message),
//                    message: "Must provide a non-empty fault message.");

//            return new Result<T>(default, false, message, exception: null);
//        }

//        /// <summary>
//        ///     Creates a <see cref="Result{T}"/> of a failed operation
//        ///     with a caught exception.
//        /// </summary>
//        /// <param name="exception">
//        ///     The exception that occured.
//        /// </param>
//        /// <param name="message">
//        ///     Message describing the failure. Optional.
//        /// </param>
//        /// <exception cref="ArgumentNullException">
//        ///     <paramref name="exception"/> was <see langword="null"/>.
//        /// </exception>
//        public static Result<T> Fault(Exception exception, string? message = null)
//        {
//            if (exception is null)
//                throw new ArgumentNullException(nameof(exception));

//            return new Result<T>(default, false, message, exception);
//        }

//        /// <summary>
//        ///     Creates a <see cref="Result{T}"/> of a successful operation.
//        /// </summary>
//        /// <param name="value">
//        ///     The result object.
//        /// </param>
//        /// <param name="message">
//        ///     Additional info message, optional.
//        /// </param>
//        public static Result<T> Success(T value, string? message = null)
//            => new Result<T>(value, true, message, exception: null);

//        //public static explicit operator Result<T>(Result other)
//        //{
//        //    if (other.IsSuccess)
//        //        throw new InvalidOperationException("Only a failed non-generic result can be converted to the generic version");

//        //    return new Result<T>(default, other.Message, false);
//        //}

//        public static implicit operator Result(Result<T> result)
//            => result._isInited
//                ? new Result(result._isSuccess, result._message, result._exception)
//                : default;
//    }
//}

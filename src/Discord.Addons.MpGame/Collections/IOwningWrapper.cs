//using System;

//namespace Discord.Addons.MpGame.Collections
//{
//    interface IOwningWrapper<T> : IWrapper<T>
//        where T : class
//    {
//        /// <summary>
//        ///     Called when ownership of the wrapper has transfered from one pile to a different pile.
//        /// </summary>
//        /// <param name="newPile">
//        ///     The new Pile that now owns this wrapper.
//        /// </param>
//        void Reset<TWrapper>(OwningPile<T, TWrapper> newPile)
//            where TWrapper : struct, IOwningWrapper<T>;
//    }

//    //    struct FlipWrapper<T> : IOwningWrapper<T>
//    //        where T : class
//    //    {
//    //        public void Reset<TWrapper>(OwningPile<T, TWrapper> newPile)
//    //            where TWrapper : struct, IOwningWrapper<T>
//    //        {
//    //            throw new NotImplementedException();
//    //        }

//    //        public T Unwrap(bool revealing)
//    //        {
//    //            throw new NotImplementedException();
//    //        }
//    //    }

//    //    abstract class FlipPile<T> : OwningPile<T, FlipWrapper<T>>
//    //        where T : class
//    //    {

//    //    }
//}

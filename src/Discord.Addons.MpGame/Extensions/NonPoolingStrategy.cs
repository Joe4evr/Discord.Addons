//using System;

//namespace Discord.Addons.MpGame.Collections
//{
//    public abstract partial class Pile<TCard>
//    {
//        private sealed class NonPoolingStrategy : IBufferStrategy<TCard>
//        {
//            public static NonPoolingStrategy Instance { get; } = new NonPoolingStrategy();

//            private NonPoolingStrategy() { }

//            public TCard[] GetBuffer(int size) => new TCard[size];
//            public void ReturnBuffer(TCard[] buffer) { } //explicit no-op
//        }
//    }
//}

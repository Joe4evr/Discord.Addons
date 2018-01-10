using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Discord.WebSocket
{
    public sealed class CacheLinkedMessageCollection : IDictionary<ulong, IMessage>
    {
        //private delegate bool MessageUncached(ulong messageId);

        //private static event Func<ulong, bool> OnMessageUncached;

        //// it's not possible to fire an event
        //// outside of the containing class
        //internal static void UncacheMessage(IMessage message)
        //    => OnMessageUncached?.Invoke(message.Id);

        private readonly ConcurrentDictionary<ulong, IMessage> _backing = new ConcurrentDictionary<ulong, IMessage>();

        public CacheLinkedMessageCollection(DiscordSocketClient client)
        {
            //_client.MessageUncached += TryRemoveMessage;
        }

        public bool TryAddMessage(IMessage message)
            => _backing.TryAdd(message.Id, message);

        public bool TryGetMessage(ulong id, out IMessage message)
            => _backing.TryGetValue(id, out message);

        public bool TryRemoveMessage(ulong id)
            => _backing.TryRemove(id, out _);

        #region IDictionary/ICollection/IEnumerable
        ICollection<ulong> IDictionary<ulong, IMessage>.Keys
            => _backing.Keys;

        ICollection<IMessage> IDictionary<ulong, IMessage>.Values
            => _backing.Values;

        int ICollection<KeyValuePair<ulong, IMessage>>.Count
            => _backing.Count;

        bool ICollection<KeyValuePair<ulong, IMessage>>.IsReadOnly
            => ((ICollection<KeyValuePair<ulong, IMessage>>)_backing).IsReadOnly;

        IMessage IDictionary<ulong, IMessage>.this[ulong key]
        {
            get => _backing[key];
            set => _backing[key] = value;
        }

        void IDictionary<ulong, IMessage>.Add(ulong key, IMessage value)
            => TryAddMessage(value);

        bool IDictionary<ulong, IMessage>.ContainsKey(ulong key)
            => _backing.ContainsKey(key);

        bool IDictionary<ulong, IMessage>.Remove(ulong key)
            => TryRemoveMessage(key);

        bool IDictionary<ulong, IMessage>.TryGetValue(ulong key, out IMessage value)
            => TryGetMessage(key, out value);

        void ICollection<KeyValuePair<ulong, IMessage>>.Add(KeyValuePair<ulong, IMessage> item)
            => TryAddMessage(item.Value);

        void ICollection<KeyValuePair<ulong, IMessage>>.Clear()
            => _backing.Clear();

        bool ICollection<KeyValuePair<ulong, IMessage>>.Contains(KeyValuePair<ulong, IMessage> item)
            => _backing.ContainsKey(item.Key);

        void ICollection<KeyValuePair<ulong, IMessage>>.CopyTo(KeyValuePair<ulong, IMessage>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<ulong, IMessage>>)_backing).CopyTo(array, arrayIndex);

        bool ICollection<KeyValuePair<ulong, IMessage>>.Remove(KeyValuePair<ulong, IMessage> item)
            => TryRemoveMessage(item.Key);

        IEnumerator<KeyValuePair<ulong, IMessage>> IEnumerable<KeyValuePair<ulong, IMessage>>.GetEnumerator()
            => _backing.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _backing.GetEnumerator();
        #endregion

        ~CacheLinkedMessageCollection()
        {
            //_client.MessageUncached -= TryRemoveMessage;
        }
    }
}

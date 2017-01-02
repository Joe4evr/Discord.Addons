using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Discord.Addons.MpGame
{
    /// <summary>
    /// Represents a circular doubly linked list.
    /// </summary>
    /// <typeparam name="T">Specifies the element type of the linked list.</typeparam>
    /// <remarks>This code copied from https://navaneethkn.wordpress.com/2009/08/18/circular-linked-list/ </remarks>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class CircularLinkedList<T> : IReadOnlyCollection<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IEqualityComparer<T> _comparer;

        /// <summary>
        /// Initializes a new instance of <see cref="CircularLinkedList{T}"/>
        /// </summary>
        /// <param name="collection">Collection of objects that will be added to linked list</param>
        public CircularLinkedList(IEnumerable<T> collection)
            : this(collection, EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CircularLinkedList{T}"/>
        /// </summary>
        /// <param name="collection">Collection of objects that will be added to linked list</param>
        /// <param name="comparer">Comparer used for item comparison</param>
        public CircularLinkedList(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            _comparer = comparer;
            if (collection != null)
            {
                foreach (T item in collection)
                    AddLast(item);
            }

            Count = collection.Count();
        }

        /// <summary>
        /// Gets Tail node. Returns NULL if no tail node found
        /// </summary>
        public Node<T> Tail { get; private set; }

        /// <summary>
        /// Gets the head node. Returns NULL if no node found
        /// </summary>
        public Node<T> Head { get; private set; }

        /// <summary>
        /// Gets total number of items in the list
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the item at the current index
        /// </summary>
        /// <param name="index">Zero-based index</param>
        /// <exception cref="ArgumentOutOfRangeException">index is out of range</exception>
        public Node<T> this[int index]
        {
            get
            {
                if (index >= Count || index < 0) throw new ArgumentOutOfRangeException("index");
                else
                {
                    Node<T> node = Head;
                    for (int i = 0; i < index; i++)
                        node = node.Next;

                    return node;
                }
            }
        }

        /// <summary>
        /// Add a new item to the end of the list
        /// </summary>
        /// <param name="item">Item to be added</param>
        private void AddLast(T item)
        {
            // if head is null, then this will be the first item
            if (Head == null)
                AddFirstItem(item);
            else
            {
                Node<T> newNode = new Node<T>(item);
                Tail.Next = newNode;
                newNode.Next = Head;
                newNode.Previous = Tail;
                Tail = newNode;
                Head.Previous = Tail;
            }
        }

        private void AddFirstItem(T item)
        {
            Head = new Node<T>(item);
            Tail = Head;
            Head.Next = Tail;
            Head.Previous = Tail;
        }

        /// <summary>
        /// Adds item to the last
        /// </summary>
        /// <param name="item">Item to be added</param>
        private void AddFirst(T item)
        {
            if (Head == null)
                AddFirstItem(item);
            else
            {
                Node<T> newNode = new Node<T>(item);
                Head.Previous = newNode;
                newNode.Previous = Tail;
                newNode.Next = Head;
                Tail.Next = newNode;
                Head = newNode;
            }
        }

        /// <summary>
        /// Adds the specified item after the specified existing node in the list.
        /// </summary>
        /// <param name="node">Existing node after which new item will be inserted</param>
        /// <param name="item">New item to be inserted</param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is NULL</exception>
        /// <exception cref="InvalidOperationException"><paramref name="node"/> doesn't belongs to list</exception>
        private void AddAfter(Node<T> node, T item)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            // ensuring the supplied node belongs to this list
            Node<T> temp = FindNode(Head, node.Value);
            if (temp != node)
                throw new InvalidOperationException("Node doesn't belongs to this list");

            Node<T> newNode = new Node<T>(item);
            newNode.Next = node.Next;
            node.Next.Previous = newNode;
            newNode.Previous = node;
            node.Next = newNode;

            // if the node adding is tail node, then re-pointing tail node
            if (node == Tail)
                Tail = newNode;
        }

        /// <summary>
        /// Adds the new item after the specified existing item in the list.
        /// </summary>
        /// <param name="existingItem">Existing item after which new item will be added</param>
        /// <param name="newItem">New item to be added to the list</param>
        /// <exception cref="ArgumentException"><paramref name="existingItem"/> doesn't exist in the list</exception>
        private void AddAfter(T existingItem, T newItem)
        {
            // finding a node for the existing item
            Node<T> node = Find(existingItem);
            if (node == null)
                throw new ArgumentException("existingItem doesn't exist in the list");
            AddAfter(node, newItem);
        }

        /// <summary>
        /// Adds the specified item before the specified existing node in the list.
        /// </summary>
        /// <param name="node">Existing node before which new item will be inserted</param>
        /// <param name="item">New item to be inserted</param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is NULL</exception>
        /// <exception cref="InvalidOperationException"><paramref name="node"/> doesn't belongs to list</exception>
        private void AddBefore(Node<T> node, T item)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            // ensuring the supplied node belongs to this list
            Node<T> temp = FindNode(Head, node.Value);
            if (temp != node)
                throw new InvalidOperationException("Node doesn't belongs to this list");

            Node<T> newNode = new Node<T>(item);
            node.Previous.Next = newNode;
            newNode.Previous = node.Previous;
            newNode.Next = node;
            node.Previous = newNode;

            // if the node adding is head node, then re-pointing head node
            if (node == Head)
                Head = newNode;
        }

        /// <summary>
        /// Adds the new item before the specified existing item in the list.
        /// </summary>
        /// <param name="existingItem">Existing item before which new item will be added</param>
        /// <param name="newItem">New item to be added to the list</param>
        /// <exception cref="ArgumentException"><paramref name="existingItem"/> doesn't exist in the list</exception>
        private void AddBefore(T existingItem, T newItem)
        {
            // finding a node for the existing item
            Node<T> node = Find(existingItem);
            if (node == null)
                throw new ArgumentException("existingItem doesn't exist in the list");
            AddBefore(node, newItem);
        }

        /// <summary>
        /// Finds the supplied item and returns a node which contains item. Returns NULL if item not found
        /// </summary>
        /// <param name="item">Item to search</param>
        /// <returns><see cref="Node&lt;T&gt;"/> instance or NULL</returns>
        public Node<T> Find(T item)
        {
            Node<T> node = FindNode(Head, item);
            return node;
        }

        private Node<T> FindNode(Node<T> node, T valueToCompare)
        {
            Node<T> result = null;
            if (_comparer.Equals(node.Value, valueToCompare))
                result = node;
            else if (result == null && node.Next != Head)
                result = FindNode(node.Next, valueToCompare);
            return result;
        }

        /// <summary>
        /// Gets a forward enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            Node<T> current = Head;
            if (current != null)
            {
                do
                {
                    yield return current.Value;
                    current = current.Next;
                } while (current != Head);
            }
        }

        /// <summary>
        /// Gets a reverse enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetReverseEnumerator()
        {
            Node<T> current = Tail;
            if (current != null)
            {
                do
                {
                    yield return current.Value;
                    current = current.Previous;
                } while (current != Tail);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Determines whether a value is in the list.
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <returns>TRUE if item exist, else FALSE</returns>
        public bool Contains(T item)
        {
            return Find(item) != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");

            Node<T> node = Head;
            do
            {
                array[arrayIndex++] = node.Value;
                node = node.Next;
            } while (node != Head);
        }
    }

    /// <summary>
    /// Represents a node
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Value = {Value}")]
    public sealed class Node<T>
    {
        /// <summary>
        /// Gets the Value
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets next node
        /// </summary>
        public Node<T> Next { get; internal set; }

        /// <summary>
        /// Gets previous node
        /// </summary>
        public Node<T> Previous { get; internal set; }

        /// <summary>
        /// Initializes a new <see cref="Node{T}"/> instance
        /// </summary>
        /// <param name="item">Value to be assigned</param>
        internal Node(T item)
        {
            Value = item;
        }
    }
}

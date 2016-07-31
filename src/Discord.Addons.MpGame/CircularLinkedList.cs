using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Discord.Addons.MpGame
{
    /// <summary>
    /// Represents a circular doubly linked list.
    /// </summary>
    /// <typeparam name="T">Specifies the element type of the linked list.</typeparam>
    /// <remarks>This code copied from https://navaneethkn.wordpress.com/2009/08/18/circular-linked-list/ </remarks>
    [DebuggerDisplay("Count = {Count}")]
    public sealed class CircularLinkedList<T> : ICollection<T>, IEnumerable<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Node<T> head = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        Node<T> tail = null;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        int count = 0;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        readonly IEqualityComparer<T> comparer;

        /// <summary>
        /// Initializes a new instance of <see cref="CircularLinkedList"/>
        /// </summary>
        public CircularLinkedList()
            : this(null, EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CircularLinkedList"/>
        /// </summary>
        /// <param name="collection">Collection of objects that will be added to linked list</param>
        public CircularLinkedList(IEnumerable<T> collection)
            : this(collection, EqualityComparer<T>.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CircularLinkedList"/>
        /// </summary>
        /// <param name="comparer">Comparer used for item comparison</param>
        /// <exception cref="ArgumentNullException">comparer is null</exception>
        public CircularLinkedList(IEqualityComparer<T> comparer)
            : this(null, comparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CircularLinkedList"/>
        /// </summary>
        /// <param name="collection">Collection of objects that will be added to linked list</param>
        /// <param name="comparer">Comparer used for item comparison</param>
        public CircularLinkedList(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            if (comparer == null) throw new ArgumentNullException(nameof(comparer));

            this.comparer = comparer;
            if (collection != null)
            {
                foreach (T item in collection)
                    AddLast(item);
            }
        }

        /// <summary>
        /// Gets Tail node. Returns NULL if no tail node found
        /// </summary>
        public Node<T> Tail { get { return tail; } }

        /// <summary>
        /// Gets the head node. Returns NULL if no node found
        /// </summary>
        public Node<T> Head { get { return head; } }

        /// <summary>
        /// Gets total number of items in the list
        /// </summary>
        public int Count { get { return count; } }

        /// <summary>
        /// Gets the item at the current index
        /// </summary>
        /// <param name="index">Zero-based index</param>
        /// <exception cref="ArgumentOutOfRangeException">index is out of range</exception>
        public Node<T> this[int index]
        {
            get
            {
                if (index >= count || index < 0) throw new ArgumentOutOfRangeException("index");
                else
                {
                    Node<T> node = head;
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
        public void AddLast(T item)
        {
            // if head is null, then this will be the first item
            if (head == null)
                this.AddFirstItem(item);
            else
            {
                Node<T> newNode = new Node<T>(item);
                tail.Next = newNode;
                newNode.Next = head;
                newNode.Previous = tail;
                tail = newNode;
                head.Previous = tail;
            }
            ++count;
        }

        void AddFirstItem(T item)
        {
            head = new Node<T>(item);
            tail = head;
            head.Next = tail;
            head.Previous = tail;
        }

        /// <summary>
        /// Adds item to the last
        /// </summary>
        /// <param name="item">Item to be added</param>
        public void AddFirst(T item)
        {
            if (head == null)
                this.AddFirstItem(item);
            else
            {
                Node<T> newNode = new Node<T>(item);
                head.Previous = newNode;
                newNode.Previous = tail;
                newNode.Next = head;
                tail.Next = newNode;
                head = newNode;
            }
            ++count;
        }

        /// <summary>
        /// Adds the specified item after the specified existing node in the list.
        /// </summary>
        /// <param name="node">Existing node after which new item will be inserted</param>
        /// <param name="item">New item to be inserted</param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is NULL</exception>
        /// <exception cref="InvalidOperationException"><paramref name="node"/> doesn't belongs to list</exception>
        public void AddAfter(Node<T> node, T item)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            // ensuring the supplied node belongs to this list
            Node<T> temp = this.FindNode(head, node.Value);
            if (temp != node)
                throw new InvalidOperationException("Node doesn't belongs to this list");

            Node<T> newNode = new Node<T>(item);
            newNode.Next = node.Next;
            node.Next.Previous = newNode;
            newNode.Previous = node;
            node.Next = newNode;

            // if the node adding is tail node, then repointing tail node
            if (node == tail)
                tail = newNode;
            ++count;
        }

        /// <summary>
        /// Adds the new item after the specified existing item in the list.
        /// </summary>
        /// <param name="existingItem">Existing item after which new item will be added</param>
        /// <param name="newItem">New item to be added to the list</param>
        /// <exception cref="ArgumentException"><paramref name="existingItem"/> doesn't exist in the list</exception>
        public void AddAfter(T existingItem, T newItem)
        {
            // finding a node for the existing item
            Node<T> node = this.Find(existingItem);
            if (node == null)
                throw new ArgumentException("existingItem doesn't exist in the list");
            this.AddAfter(node, newItem);
        }

        /// <summary>
        /// Adds the specified item before the specified existing node in the list.
        /// </summary>
        /// <param name="node">Existing node before which new item will be inserted</param>
        /// <param name="item">New item to be inserted</param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is NULL</exception>
        /// <exception cref="InvalidOperationException"><paramref name="node"/> doesn't belongs to list</exception>
        public void AddBefore(Node<T> node, T item)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            // ensuring the supplied node belongs to this list
            Node<T> temp = this.FindNode(head, node.Value);
            if (temp != node)
                throw new InvalidOperationException("Node doesn't belongs to this list");

            Node<T> newNode = new Node<T>(item);
            node.Previous.Next = newNode;
            newNode.Previous = node.Previous;
            newNode.Next = node;
            node.Previous = newNode;

            // if the node adding is head node, then repointing head node
            if (node == head)
                head = newNode;
            ++count;
        }

        /// <summary>
        /// Adds the new item before the specified existing item in the list.
        /// </summary>
        /// <param name="existingItem">Existing item before which new item will be added</param>
        /// <param name="newItem">New item to be added to the list</param>
        /// <exception cref="ArgumentException"><paramref name="existingItem"/> doesn't exist in the list</exception>
        public void AddBefore(T existingItem, T newItem)
        {
            // finding a node for the existing item
            Node<T> node = this.Find(existingItem);
            if (node == null)
                throw new ArgumentException("existingItem doesn't exist in the list");
            this.AddBefore(node, newItem);
        }

        /// <summary>
        /// Finds the supplied item and returns a node which contains item. Returns NULL if item not found
        /// </summary>
        /// <param name="item">Item to search</param>
        /// <returns><see cref="Node&lt;T&gt;"/> instance or NULL</returns>
        public Node<T> Find(T item)
        {
            Node<T> node = FindNode(head, item);
            return node;
        }

        /// <summary>
        /// Removes the first occurance of the supplied item
        /// </summary>
        /// <param name="item">Item to be removed</param>
        /// <returns>TRUE if removed, else FALSE</returns>
        public bool Remove(T item)
        {
            // finding the first occurance of this item
            Node<T> nodeToRemove = this.Find(item);
            if (nodeToRemove != null)
                return this.RemoveNode(nodeToRemove);
            return false;
        }

        bool RemoveNode(Node<T> nodeToRemove)
        {
            Node<T> previous = nodeToRemove.Previous;
            previous.Next = nodeToRemove.Next;
            nodeToRemove.Next.Previous = nodeToRemove.Previous;

            // if this is head, we need to update the head reference
            if (head == nodeToRemove)
                head = nodeToRemove.Next;
            else if (tail == nodeToRemove)
                tail = tail.Previous;

            --count;
            return true;
        }

        /// <summary>
        /// Removes all occurances of the supplied item
        /// </summary>
        /// <param name="item">Item to be removed</param>
        public void RemoveAll(T item)
        {
            bool removed = false;
            do
            {
                removed = this.Remove(item);
            } while (removed);
        }

        /// <summary>
        /// Clears the list
        /// </summary>
        public void Clear()
        {
            head = null;
            tail = null;
            count = 0;
        }

        /// <summary>
        /// Removes head
        /// </summary>
        /// <returns>TRUE if successfully removed, else FALSE</returns>
        public bool RemoveHead()
        {
            return this.RemoveNode(head);
        }

        /// <summary>
        /// Removes tail
        /// </summary>
        /// <returns>TRUE if successfully removed, else FALSE</returns>
        public bool RemoveTail()
        {
            return this.RemoveNode(tail);
        }

        Node<T> FindNode(Node<T> node, T valueToCompare)
        {
            Node<T> result = null;
            if (comparer.Equals(node.Value, valueToCompare))
                result = node;
            else if (result == null && node.Next != head)
                result = FindNode(node.Next, valueToCompare);
            return result;
        }

        /// <summary>
        /// Gets a forward enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            Node<T> current = head;
            if (current != null)
            {
                do
                {
                    yield return current.Value;
                    current = current.Next;
                } while (current != head);
            }
        }

        /// <summary>
        /// Gets a reverse enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetReverseEnumerator()
        {
            Node<T> current = tail;
            if (current != null)
            {
                do
                {
                    yield return current.Value;
                    current = current.Previous;
                } while (current != tail);
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

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");

            Node<T> node = this.head;
            do
            {
                array[arrayIndex++] = node.Value;
                node = node.Next;
            } while (node != head);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<T>.Add(T item)
        {
            this.AddLast(item);
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
        public T Value { get; private set; }

        /// <summary>
        /// Gets next node
        /// </summary>
        public Node<T> Next { get; internal set; }

        /// <summary>
        /// Gets previous node
        /// </summary>
        public Node<T> Previous { get; internal set; }

        /// <summary>
        /// Initializes a new <see cref="Node"/> instance
        /// </summary>
        /// <param name="item">Value to be assigned</param>
        internal Node(T item)
        {
            this.Value = item;
        }
    }
}

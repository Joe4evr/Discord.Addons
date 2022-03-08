using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Discord.Addons.Core;

namespace Discord.Addons.MpGame.Collections;

/// <summary>
///     Represents a circular doubly linked list.
/// </summary>
/// <typeparam name="T">
///     Specifies the element type of the linked list.
/// </typeparam>
/// <remarks>
///     This code adapted from https://navaneethkn.wordpress.com/2009/08/18/circular-linked-list/
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
public sealed class CircularLinkedList<T> : IReadOnlyCollection<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal IEqualityComparer<T> Comparer { get; }

    private readonly object _lock = new();

    /// <summary>
    ///     Initializes a new instance of <see cref="CircularLinkedList{T}"/>
    /// </summary>
    /// <param name="collection">
    ///     Collection of objects that will be added to linked list
    /// </param>
    public CircularLinkedList(IEnumerable<T> collection)
        : this(collection, EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="CircularLinkedList{T}"/>
    /// </summary>
    /// <param name="collection">
    ///     Collection of objects that will be added to linked list
    /// </param>
    /// <param name="comparer">
    ///     Comparer used for item comparison
    /// </param>
    public CircularLinkedList(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        if (collection != null)
        {
            foreach (var item in collection)
                AddLast(item);
        }

        //_count = collection?.Count() ?? 0;
    }

    /// <summary>
    ///     Gets the head node. Returns <see langword="null"/> if no node found
    /// </summary>
    public Node<T> Head => _head!;
    private Node<T>? _head;

    /// <summary>
    ///     Gets Tail node. Returns <see langword="null"/> if no node found
    /// </summary>
    public Node<T> Tail => _tail!;
    private Node<T>? _tail;

    /// <summary>
    ///     Gets total number of items in the list
    /// </summary>
    public int Count => _count;
    private int _count = 0;

    /// <summary>
    ///     Gets the item at the current index
    /// </summary>
    /// <param name="index">
    ///     Zero-based index
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="index"/> is out of range
    /// </exception>
    public Node<T> this[int index]
    {
        get
        {
            if (index >= Count || index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            else
            {
                var node = Head;
                for (int i = 0; i < index; i++)
                    node = node.Next;

                return node;
            }
        }
    }

    /// <summary>
    ///     Add a new item to the end of the list
    /// </summary>
    /// <param name="item">
    ///     Item to be added
    /// </param>
    internal void AddLast(T item)
    {
        lock (_lock)
        {
            // if head is null, then this will be the first item
            if (Head is null)
            {
                AddFirstItem(item);
            }
            else
            {
                var newNode = new Node<T>(item);
                Tail.Next = newNode;
                newNode.Next = Head;
                newNode.Previous = Tail;
                _tail = newNode;
                Head.Previous = Tail;
            }
            Interlocked.Increment(ref _count);
        }
    }

    private void AddFirstItem(T item)
    {
        _head = new Node<T>(item);
        _tail = Head;
        Head.Next = Tail;
        Head.Previous = Tail;
    }

    ///// <summary>
    /////     Adds item to the list
    ///// </summary>
    ///// <param name="item">
    /////     Item to be added
    ///// </param>
    //private void AddFirst(T item)
    //{
    //    if (Head is null)
    //        AddFirstItem(item);
    //    else
    //    {
    //        var newNode = new Node<T>(item);
    //        Head.Previous = newNode;
    //        newNode.Previous = Tail;
    //        newNode.Next = Head;
    //        Tail.Next = newNode;
    //        _head = newNode;
    //    }
    //}

    ///// <summary>
    /////     Adds the specified item after the specified existing node in the list.
    ///// </summary>
    ///// <param name="node">
    /////     Existing node after which new item will be inserted
    ///// </param>
    ///// <param name="item">
    /////     New item to be inserted
    ///// </param>
    ///// <exception cref="ArgumentNullException">
    /////     <paramref name="node"/> is <see langword="null"/>
    ///// </exception>
    ///// <exception cref="InvalidOperationException">
    /////     <paramref name="node"/> doesn't belongs to list
    ///// </exception>
    //private void AddAfter(Node<T> node, T item)
    //{
    //    if (node is null)
    //        throw new ArgumentNullException(nameof(node));
    //    // ensuring the supplied node belongs to this list
    //    var temp = Find(node.Value);
    //    if (temp != node)
    //        throw new InvalidOperationException("Node doesn't belongs to this list");

    //    var newNode = new Node<T>(item)
    //    {
    //        Next = node.Next
    //    };
    //    node.Next.Previous = newNode;
    //    newNode.Previous = node;
    //    node.Next = newNode;

    //    // if the node adding is tail node, then re-pointing tail node
    //    if (node == Tail)
    //        _tail = newNode;
    //}

    ///// <summary>
    /////     Adds the specified item before the specified existing node in the list.
    ///// </summary>
    ///// <param name="node">
    /////     Existing node before which new item will be inserted
    ///// </param>
    ///// <param name="item">
    /////     New item to be inserted
    ///// </param>
    ///// <exception cref="ArgumentNullException">
    /////     <paramref name="node"/> is <see langword="null"/>
    ///// </exception>
    ///// <exception cref="InvalidOperationException">
    /////     <paramref name="node"/> doesn't belongs to list
    ///// </exception>
    //private void AddBefore(Node<T> node, T item)
    //{
    //    if (node is null)
    //        throw new ArgumentNullException(nameof(node));
    //    // ensuring the supplied node belongs to this list
    //    var temp = Find(node.Value);
    //    if (temp != node)
    //        throw new InvalidOperationException("Node doesn't belongs to this list");

    //    var newNode = new Node<T>(item);
    //    node.Previous.Next = newNode;
    //    newNode.Previous = node.Previous;
    //    newNode.Next = node;
    //    node.Previous = newNode;

    //    // if the node adding is head node, then re-pointing head node
    //    if (node == Head)
    //        _head = newNode;
    //}

    /// <summary>
    ///     Finds the supplied item and returns a node which contains item.
    ///     Returns <see langword="null"/> if item not found
    /// </summary>
    /// <param name="item">
    ///     Item to search
    /// </param>
    /// <returns>
    ///     <see cref="Node{T}"/> instance or <see langword="null"/>
    /// </returns>
    public Node<T>? Find(T item)
    {
        for (var current = Head; current.Next != Head; current = current.Next)
        {
            if (Comparer.Equals(current.Value, item))
                return current;
        }
        return null;
    }

    /// <summary>
    ///     Gets a forward enumerator
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T> GetEnumerator()
    {
        var current = Head;
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
    ///     Gets a reverse enumerator
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T> GetReverseEnumerator()
    {
        var current = Tail;
        if (current != null)
        {
            do
            {
                yield return current.Value;
                current = current.Previous;
            } while (current != Tail);
        }
    }

    /// <summary>
    ///     Determines whether a value is in the list.
    /// </summary>
    /// <param name="item">
    ///     Item to check
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if item exist, else <see langword="false"/>.
    /// </returns>
    public bool Contains(T item)
        => Find(item) != null;

    internal bool RemoveItem(T item)
    {
        lock (_lock)
        {
            //var temp = Find(item);
            //if (temp != null)
            if (Find(item) is { } temp)
            {
                if (Count > 1)
                {
                    if (Comparer.Equals(item, Head!.Value))
                        _head = temp.Next;
                    if (Comparer.Equals(item, Tail!.Value))
                        _tail = temp.Previous;

                    temp.Previous!.Next = temp.Next;
                    temp.Next!.Previous = temp.Previous;
                }

                temp.Next = null!;
                temp.Previous = null!;

                Interlocked.Decrement(ref _count);
                return true;
            }
        }
        return false;
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        => GetEnumerator();
}

/// <summary>
///     Represents a node
/// </summary>
[DebuggerDisplay("Value = {Value}")]
public sealed class Node<T>
{
    /// <summary>
    ///     Gets the Value
    /// </summary>
    public T Value => ThrowIfNextOnly().ReturnWith(_value);

    /// <summary>
    ///     Gets next node
    /// </summary>
    public Node<T> Next
    {
        get => _next!;
        internal set => _next = value;
    }

    /// <summary>
    ///     Gets previous node
    /// </summary>
    public Node<T> Previous
    {
        get => ThrowIfNextOnly().ReturnWith(_previous!);
        internal set => _previous = value;
    }

    /// <summary>
    ///     Initializes a new <see cref="Node{T}"/> instance
    /// </summary>
    /// <param name="item">
    ///     Value to be assigned
    /// </param>
    internal Node(T item)
    {
        _value = item;
        _isNextOnly = false;
    }

    private Node(bool nextOnly)
    {
        _value = default!;
        _isNextOnly = nextOnly;
    }

    private readonly bool _isNextOnly;
    private readonly T _value;
    private Node<T>? _next;
    private Node<T>? _previous;

    private Unit ThrowIfNextOnly()
    {
        if (_isNextOnly)
            throw new InvalidOperationException("You may only use 'Next' on this 'Node<T>' instance.");

        return default;
    }

    internal static Node<T> CreateNextOnlyNode(Node<T> next)
        => new(nextOnly: true) { Next = next };
}

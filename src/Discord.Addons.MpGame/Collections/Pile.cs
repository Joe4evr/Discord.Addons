using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Discord.Addons.MpGame.Collections
{
    /// <summary>
    /// Base type to represent a pile of objects,
    /// specifically for use in card games.
    /// </summary>
    /// <typeparam name="TCard">The card type.</typeparam>
    /// <remarks><div class="markdown level0 remarks"><div class="CAUTION">
    /// <h5>Caution</h5><p>This class is not thread-safe.</p></div></div></remarks>
    [DebuggerDisplay("Count = {Count}")]
    public abstract partial class Pile<TCard>
        where TCard : class
    {
        //private Stack<TCard> _top;
        //private Queue<TCard> _bottom;
        //private bool _strategyUsed = false;
        //private IBufferStrategy<TCard> _bufferStrategy = NonPoolingStrategy.Instance;

        private Node _head;
        private Node _tail;
        private int _count = 0;

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> to an empty state.
        /// </summary>
        protected Pile()
        {
            //_top = new Stack<TCard>();
            //_bottom = new Queue<TCard>();
        }

        /// <summary>
        /// Initializes a new <see cref="Pile{TCard}"/> with the specified cards.
        /// </summary>
        /// <param name="cards">The cards to put in the pile.</param>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>This constructor will filter out any items in
        /// <paramref name="cards"/> that are <see langword="null"/>.</p></div></div></remarks>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/>
        /// was <see langword="null"/>.</exception>
        protected Pile(IEnumerable<TCard> cards)
        {
            ThrowArgNull(cards, nameof(cards));

            SetNewSequence(cards);
        }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> is freely browsable.
        /// </summary>
        public abstract bool CanBrowse { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> can be cleared.
        /// </summary>
        public abstract bool CanClear { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// can be cut, ie. taking a number of cards from the top
        /// and putting them underneath the remainder.
        /// </summary>
        public abstract bool CanCut { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// allows drawing cards from the top.
        /// </summary>
        public abstract bool CanDraw { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// allows drawing cards from the bottom.
        /// </summary>
        public abstract bool CanDrawBottom { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// allows inserting cards at an arbitrary index.
        /// </summary>
        public abstract bool CanInsert { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> allows peeking at cards.
        /// </summary>
        public abstract bool CanPeek { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> allows putting cards on the top.
        /// </summary>
        public abstract bool CanPut { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> allows putting cards on the bottom.
        /// </summary>
        public abstract bool CanPutBottom { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/> can be reshuffled.
        /// </summary>
        public abstract bool CanShuffle { get; }

        /// <summary>
        /// Indicates whether or not this <see cref="Pile{TCard}"/>
        /// allows taking cards from an arbitrary index.
        /// </summary>
        public abstract bool CanTake { get; }

        /// <summary>
        /// The amount of cards currently in the pile.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// A snapshot of all the cards
        /// without removing them from the pile.
        /// Requires <see cref="CanBrowse"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow browsing the cards.</exception>
        public IReadOnlyCollection<TCard> Cards
        {
            get
            {
                ThrowInvalidOp(!CanBrowse, ErrorStrings.NoBrowse);

                return GetAllInternal(clear: false);
            }
        }

        /// <summary>
        /// Clears the entire pile and returns the cards
        /// that were in it. Requires <see cref="CanClear"/>.
        /// </summary>
        /// <returns>The collection as it was before it is cleared.</returns>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow clearing all cards.</exception>
        public IReadOnlyCollection<TCard> Clear()
        {
            ThrowInvalidOp(!CanClear, ErrorStrings.NoClear);

            return GetAllInternal(clear: true);
        }

        /// <summary>
        /// Cuts the pile at a specified number of cards from the top
        /// and places the taken cards on the bottom.
        /// </summary>
        /// <param name="cutIndex">The number of cards to send to the bottom.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="cutIndex"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow cutting the pile.</exception>
        public void Cut(int cutIndex)
        {
            ThrowInvalidOp(!CanCut, ErrorStrings.NoCut);
            ThrowArgOutOfRange(cutIndex < 0, ErrorStrings.CutIndexNegative, nameof(cutIndex));
            ThrowArgOutOfRange(cutIndex > Count, ErrorStrings.CutIndexTooHigh, nameof(cutIndex));

            if (cutIndex == 0 || cutIndex == Count)
                return; //no changes

            var tmp = GetAt(cutIndex);
            _head.Previous = _tail;
            _head = tmp.Next;
            _tail = tmp;
            
        }

        /// <summary>
        /// Draws the top card from the pile. If the last card is
        /// drawn, calls <see cref="OnLastRemoved"/>.
        /// Requires <see cref="CanDraw"/>.
        /// </summary>
        /// <returns>If the pile's current size is greater than 0, the card
        /// currently at the top of the pile. Otherwise will throw.</returns>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow drawing cards OR There were no cards to draw.</exception>
        public TCard Draw()
        {
            ThrowInvalidOp(!CanDraw, ErrorStrings.NoDraw);
            ThrowInvalidOp(Count == 0, ErrorStrings.PileEmpty);

            var tmp = Break(_head);
            _head = _head.Next;

            if (Count == 0)
                OnLastRemoved();

            return tmp;
        }

        /// <summary>
        /// Draws the bottom card from the pile. If the last card is
        /// drawn, calls <see cref="OnLastRemoved"/>.
        /// Requires <see cref="CanDrawBottom"/>.
        /// </summary>
        /// <returns>If the pile's current size is greater than 0, the card
        /// currently at the bottom of the pile. Otherwise will throw.</returns>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow drawing cards OR There were no cards to draw.</exception>
        public TCard DrawBottom()
        {
            ThrowInvalidOp(!CanDraw, ErrorStrings.NoDraw);
            ThrowInvalidOp(Count == 0, ErrorStrings.PileEmpty);

            var tmp = Break(_tail);
            _tail = _tail.Previous;

            if (Count == 0)
                OnLastRemoved();

            return tmp;
        }

        /// <summary>
        /// Inserts a card at the given index. Requires <see cref="CanInsert"/>.
        /// </summary>
        /// <param name="card">The card to insert.</param>
        /// <param name="index">The index to insert at.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow inserting cards at an arbitrary location.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> was <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        public void InsertAt(TCard card, int index)
        {
            ThrowInvalidOp(!CanInsert, ErrorStrings.NoInsert);
            ThrowArgNull(card, nameof(card));
            ThrowArgOutOfRange(index < 0, ErrorStrings.InsertionNegative, nameof(index));
            ThrowArgOutOfRange(index > Count, ErrorStrings.InsertionTooHigh, nameof(index));

            if (index == 0)
            {
                AddHead(card);
            }
            else if (index == Count)
            {
                _tail = AddAfter(_tail, card);
            }
            else
            {
                AddAfter(GetAt(index), card);
            }
        }

        /// <summary>
        /// Peeks the <paramref name="amount"/> of
        /// cards without removing them from the pile.
        /// Requires <see cref="CanPeek"/>.
        /// </summary>
        /// <param name="amount">The amount of cards to peek.</param>
        /// <returns>An array of the cards currently at the top of the pile.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="amount"/>
        /// was less than 0 or greater than the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow peeking cards.</exception>
        public IReadOnlyCollection<TCard> PeekTop(int amount)
        {
            ThrowInvalidOp(!CanPeek, ErrorStrings.NoPeek);
            ThrowArgOutOfRange(amount < 0, ErrorStrings.PeekAmountNegative, nameof(amount));
            ThrowArgOutOfRange(amount > Count, ErrorStrings.PeekAmountTooHigh, nameof(amount));

            if (amount == 0)
                return ImmutableArray<TCard>.Empty;

            var result = new List<TCard>(capacity: amount);

            var tmp = _head;
            for (int i = 0; i < amount; i++)
                result.Add(tmp.Value);

            return result.ToImmutableArray();
        }

        /// <summary>
        /// Puts a card on top of the pile.
        /// Calls <see cref="OnPut(TCard)"/>.
        /// Requires <see cref="CanPut"/>.
        /// </summary>
        /// <param name="card">The card to place on top.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow placing cards onto it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> was <see langword="null"/>.</exception>
        public void Put(TCard card)
        {
            ThrowInvalidOp(!CanPut, ErrorStrings.NoPut);
            ThrowArgNull(card, nameof(card));

            AddHead(card);
            OnPut(card);
        }

        /// <summary>
        /// Puts a card on the bottom of the pile. Requires <see cref="CanPutBottom"/>.
        /// </summary>
        /// <param name="card">The card to place on the bottom.</param>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow placing cards on the bottom.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> was <see langword="null"/>.</exception>
        public void PutBottom(TCard card)
        {
            ThrowInvalidOp(!CanPutBottom, ErrorStrings.NoPutBtm);
            ThrowArgNull(card, nameof(card));

            AddTail(card);
        }

        /// <summary>
        /// Reshuffles the pile using the specified function.
        /// Requires <see cref="CanShuffle"/>.
        /// </summary>
        /// <param name="shuffleFunc">A function that produces an
        /// <see cref="IEnumerable{TCard}"/> in a new, random order.
        /// This function receives the cards currently in
        /// the pile as its argument.</param>
        /// <exception cref="InvalidOperationException">The instance
        /// does not allow reshuffling the cards OR The sequence produced from 
        /// <paramref name="shuffleFunc"/> was <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="shuffleFunc"/> was <see langword="null"/>.</exception>
        public void Shuffle(Func<IEnumerable<TCard>, IEnumerable<TCard>> shuffleFunc)
        {
            ThrowInvalidOp(!CanShuffle, ErrorStrings.NoShuffle);
            ThrowArgNull(shuffleFunc, nameof(shuffleFunc));

            var oldCount = Count;
            var shuffled = shuffleFunc(GetAllInternal(clear: false));

            ThrowInvalidOp(shuffled == null, ErrorStrings.NewSequenceNull);

            SetNewSequence(shuffled);
        }

        /// <summary>
        /// Takes a card from the given index. If the last card is
        /// drawn, calls <see cref="OnLastRemoved"/>. Requires <see cref="CanTake"/>.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <returns>The card at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
        /// was less than 0 or greater than or equal to the pile's current size.</exception>
        /// <exception cref="InvalidOperationException">The instance does not
        /// allow taking cards from an arbitrary location.</exception>
        public TCard TakeAt(int index)
        {
            ThrowInvalidOp(!CanTake, ErrorStrings.NoTake);
            ThrowArgOutOfRange(index < 0, ErrorStrings.RetrievalNegative, nameof(index));
            ThrowArgOutOfRange(index >= Count, ErrorStrings.RetrievalTooHighP, nameof(index));

            var tmp = Break(GetAt(index));

            if (Count == 0)
                OnLastRemoved();

            return tmp;
        }

        /// <summary>
        /// Automatically called when the last card is removed from the pile.
        /// </summary>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>Does nothing by default.</p></div></div></remarks>
        protected virtual void OnLastRemoved() { }

        /// <summary>
        /// Automatically called when a card is put on top of the pile.
        /// </summary>
        /// <param name="card">The card that is placed.</param>
        /// <remarks><div class="markdown level0 remarks"><div class="NOTE">
        /// <h5>Note</h5><p>Does nothing by default.</p></div></div></remarks>
        protected virtual void OnPut(TCard card) { }

        private IReadOnlyCollection<TCard> GetAllInternal(bool clear)
        {
            if (Count == 0)
                return ImmutableArray<TCard>.Empty;

            var result = new List<TCard>(capacity: Count);

            for (var n = _head; n != null; n = n.Next)
                result.Add(n.Value);

            if (clear)
            {
                _head = null;
                _tail = null;
                Interlocked.Exchange(ref _count, 0);
            }

            return result.ToImmutableArray();
        }

        private Node GetAt(int index)
        {
            if (index == 0)
                return _head;
            if (index == Count - 1)
                return _tail;

            var tmp = _head;
            for (int i = 0; i < index; i++)
                tmp = tmp.Next;

            return tmp;
        }
        private Node AddHead(TCard card)
        {
            var tmp = new Node(card)
            {
                Next = _head
            };

            if (_head != null)
                _head.Previous = tmp;

            _head = tmp;
            Interlocked.Increment(ref _count);
            //_count++;
            return tmp;
        }
        private Node AddTail(TCard card)
        {
            var tmp = new Node(card)
            {
                Previous = _tail
            };

            if (_tail != null)
                _tail.Next = tmp;

            _tail = tmp;
            Interlocked.Increment(ref _count);
            //_count++;
            return tmp;
        }
        private Node AddAfter(Node node, TCard card)
        {
            var tmp = new Node(card)
            {
                Next = node?.Next,
                Previous = node
            };

            node.Next = tmp;
            Interlocked.Increment(ref _count);
            //_count++;
            return tmp;
        }
        private TCard Break(Node node)
        {
            if (node.Previous != null)
                node.Previous.Next = node.Next;
            if (node.Next != null)
                node.Next.Previous = node.Previous;

            Interlocked.Decrement(ref _count);
            //_count--;
            return node.Value;
        }

        private void SetNewSequence(IEnumerable<TCard> cards)
        {
            _head = null;
            _tail = null;
            Interlocked.Exchange(ref _count, 0);
            foreach (var item in cards.Where(c => c != null))
            {
                _ = (_head == null)
                    ? (_tail = AddHead(item))
                    : AddTail(item);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOp(bool check, string msg)
        {
            if (check)
                throw new InvalidOperationException(message: msg);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgNull<T>(T arg, string argname)
            where T : class
        {
            if (arg == null)
                throw new ArgumentNullException(paramName: argname);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgOutOfRange(bool check, string msg, string argname)
        {
            if (check)
                throw new ArgumentOutOfRangeException(message: msg, paramName: argname);
        }

        private sealed class Node
        {
            public Node Next     { get; set; }
            public Node Previous { get; set; }
            public TCard Value   { get; }

            public Node(TCard value)
            {
                Value = value ?? throw new ArgumentNullException(nameof(value));
            }
        }
    }
}

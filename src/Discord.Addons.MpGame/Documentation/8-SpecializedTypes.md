Specialized types
=================

There are a couple of types for specific purposes shipped in MpGame.

* `CircularLinkedList<T>`: This collection is used to store all the players
for a given Game instance. It is very unlikely that you'll need to create/manipulate
an instance of this directly.
* `Pile<TCard>`: This abstract collection is optimized for storing card types with various
rules around how the cards are seen. If you are creating a game that uses cards,
consider inheriting from `Pile<TCard>` so that the behavior is adequately specified.
Any attempt to use a method that is outside of the specified behavior,
will throw `InvalidOperationException`.
```cs
internal class CardDeck : Pile<Card>
{
    //Use the base's IEnumerable constructor so that
    //the collection always gets populated.
    //(Not doing this only allows it to be
    //initialized with an empty collection.)
    internal CardDeck(IEnumerable<Card> cards)
        : base(cards)
    {
    }

    //A deck is a private zone, it may not
    //be freely browsed at will.
    public override bool CanBrowse     => false;

    //In this game, it's not uncommon to have
    //the entire deck emptied for some reason.
    public override bool CanClear      => true;

    //It is allowed to Cut the deck.
    public override bool CanCut        => true;

    //It needs to be possible to draw the top card,
    //otherwise why is this a deck of cards?
    public override bool CanDraw       => true;

    //You can't draw from the bottom card of the deck.
    //(This is exceeedingly rare, but Exploding Kittens
    //is one game that would require this.)
    public override bool CanDrawBottom => false;

    //You can't just insert a card at an arbitrary place.
    public override bool CanInsert     => false;

    //It's common enough that a player can peek
    //at the top X cards to see what is coming up.
    public override bool CanPeek       => true;

    //You can't put cards on the top of the pile.
    //(Zones like a "Graveyard"/"Discard pile"/what-have-you
    //are where you want this.)
    public override bool CanPut        => false;

    //You can't put cards on the bottom of the pile, either.
    public override bool CanPutBottom  => false;

    //This deck can be reshuffled.
    public override bool CanShuffle    => true;

    //It's not allowed to take a card from an
    //arbitrary place in the deck.
    public override bool CanTake       => false;
}
```
* `Hand<TCard>`: A companion type to `Pile<TCard>`, this collection is specifically
for representing cards in a player's "hand". This class is sealed because it doesn't
warrant any customization in behavior. In usage, make sure to never share instances of this
to types that don't need to know the contents.


[<- Part 7 - Extra considerations](7-ExtraConsiderations.md) - Specialized types

Intro to Discord.Addons.MpGame
==============================

Discord.Addons.MpGame allows you to write text-based games for Discord in a relatively easy fashion.
In order to use this library, you'll need to write some types of your own:

Required:
- A `Game` type, that contains all the logic of actually playing a game
- A `Module` type, that contains the commands needed to interact with a game

Optional:
- A `Player` type, if a player needs to keep any state at all
- A `GameService` type, if you need to manage data outside of a game instance
- Any other object type you need for your game

Intro - [Part 2 - Players ->](2-Players.md)

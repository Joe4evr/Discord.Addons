## Discord.Addons.Trivia
Easy implementation for Trivia games using Discord.Net 1.0.

### Quickstart
You'll need to write a Module that inherits from `TriviaModuleBase`, and implement
the two abstract methods as commands. One is to start a new game, and the other
is to stop a game before it's done.
```cs
// Optional: [Group("trivia")]
public class MyTrivia : TriviaModuleBase
{
    // Pass the TriviaService instance up to the base class
    public MyTrivia(TriviaService service) : base(service)
    {
    }

    [Command("start")]
    public override async Task NewGameCmd(int turns)
    {
        // You should check if there's not already
        // a game going on in this channel
        if(GameInProgress)
        {
            await ReplyAsync("Already playing");
            return;
        }

        var game = new TriviaGame(Service.TriviaData, Context.Channel, turns);
        if (Service.AddNewGame(Context.Channel.Id, game))
        {
            await game.Start();
        }
    }

    // You may want to limit this command to mods or admins.
    [Command("stop")]
    public override async Task EndGameCmd()
    {
        // Here check if there IS a game going on.
        if (GameInProgress)
        {
            await Game.End();
        }
        else
        {
            await ReplyAsync("No game going on.");
        }
    }
}
```

You also have to supply your own questions and answers. The easiest
option is to have them formatted as JSON and piggyback off of Json.Net
that already gets installed with Discord.Net. You can put multiple
answers per question in case there are certain variations that you want to
count as correct. If there's only one correct answer, it should just be a
single element array. For example:
```json
{
  "Who was the Japanese general that united most of the warring states?": [ "Oda Nobunaga", "Nobunaga Oda" ],
  "What's the most awesome Discord API wrapper?": [ "Discord.Net" ]
}
```
Answers are compared ignoring capital letters, so users won't
have to remember to case their answers properly.
(They will, however, need to able to spell correctly.)

In order to add the service to the client, call the extension method on `CommandService`:
```cs
// If using a json file:
var trivia = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText(path_to_json_file));
// The extension method works for both 'DiscordSocketClient' and 'DiscordShardedClient'.
await _commands.AddTrivia<MyTrivia>(_client, _map, trivia, _logger); // The logger argument is optional.
```

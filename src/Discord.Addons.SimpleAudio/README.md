# Discord.Addons.SimpleAudio
Easy-to-use implementation for playing music with Discord.Net 1.0.

## Prerequisites
You need FFMpeg, opus, and libsodium. You can download these
for your platform [here](https://dsharpplus.emzi0767.com/natives/).
*You need to rename `libopus.dll` to `opus.dll` before use.*

* Add opus and libsodium by right-clicking your project in
Solution Explorer -> Add -> Existing Item. Go to their
properties and set them to Copy Always.

![](https://i.imgur.com/vfSSKM1.png)

## Quickstart
1. If you want your users to be able to use commands to control the audio player,
create a new class to manage those commands.
```cs
using Discord.Addons.SimpleAudio;

public class AudioModuleImplementation : AudioModule
{
    // Add the [Command] attribute, remember to set all your commands
    // in this module to 'Runmode.Async'.
    // Add preconditions if you want more granular control.
    [Command("join", RunMode = RunMode.Async)]
    public override Task JoinCmd(IVoiceChannel target = null)
    {
        // All you need to do is call the base version
        // of the commmand.
        return base.JoinCmd(target);
    }
}
```

2. During your bot initialization, create an `AudioConfig` object that
contains at the very least, the path for `ffmpeg.exe` and
the base path for the music you want to play:
```cs
var audioCfg = new AudioConfig(
    ffmpegPath: @"C:\ffmpeg-folder\bin\ffmpeg.exe",
    musicBasePath: @"C:\bot_music")
{
    // You can provide guild-specific configurations
    // by using the following property and syntax sugar:
    GuildConfigs =
    {
        // C#6+ syntax
        [guildId] = new StandardAudioGuildConfig
        {
            // Consult IntelliSense for the
            // properties you can set here.
            AutoPlay = true
        }
    },

    // You can also set some properties globally
    // NOTE: If a guild-specific option is specified,
    // that takes precedence over the global option.
    AutoPlay = false
};
```

3. Finally, add the `AudioService` to your service collection and
your module implementation to your `CommandService`:
```cs
_serviceCollection.AddSingleton(new AudioService(_client, audioCfg, _logger));
//...
await _commands.AddModuleAsync<AudioModuleImplementation>();
```

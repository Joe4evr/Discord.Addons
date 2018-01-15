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
contains at the very least, the base path for `ffmpeg.exe` and
the base path for the music you want to play:
```cs
var audioCfg = new AudioConfig(
    // Do NOT include 'ffmpeg.exe' as part of this path
    ffmpegPath: @"C:\ffmpeg-folder\bin",
    musicBasePath: @"C:\bot_music")
{
    // You can provide guild-specific configurations
    // by using the following property:
    GuildConfigs =
    {
        [guildId] = new AudioGuildConfig
        {
            // Consult IntelliSense for the
            // properties you can set here.
        }
    }
};
```

3. Finally, call the extension method to add SimpleAudio to your `CommandService`:
```cs
await _commands.UseAudio<AudioModuleImplementation>(_client, _serviceCollection, audioCfg, _logger);
```

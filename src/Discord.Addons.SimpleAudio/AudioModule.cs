using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Preconditions;
using Discord.Commands;

namespace Discord.Addons.SimpleAudio
{
    [RequireContext(ContextType.Guild)]
    public abstract class AudioModule : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _service;

        protected AudioModule(AudioService service)
        {
            _service = service;
        }

        [ClientNotInVoice]
        public virtual async Task JoinCmd(IVoiceChannel target = null)
        {
            target = target ?? (Context.User as IVoiceState).VoiceChannel;
            var self = (Context.Guild.CurrentUser);
            if (!self.HasPerms(target, AudioExt.DiscordPermissions.CONNECT))
            {
                await ReplyAsync("I can't connect to that channel.").ConfigureAwait(false);
            }
            //else if (!self.HasPerms(target, AudioExt.DiscordPermissions.SPEAK))
            //{
            //    await ReplyAsync("I can't play in that channel.").ConfigureAwait(false);
            //}
            else
            {
                await _service.JoinAudio(Context.Guild, target).ConfigureAwait(false);
            }
        }

        //[ClientInVoice]
        //public virtual Task SwitchCmd(IVoiceChannel target)
        //{
        //    return _service.SwitchAudio(Context.Guild, target);
        //}

        [ClientInVoice]
        public virtual Task LeaveCmd()
        {
            _service.StopPlaying(Context.Guild);
            return _service.LeaveAudio(Context.Guild);
        }

        [ClientInVoice]
        public virtual Task ListCmd()
        {
            return ReplyAsync($"```\n{String.Join("\n", _service.GetAvailableFiles())}\n```");
        }

        [ClientInVoice]
        public virtual Task PlayCmd([Remainder] string song)
        {
            return _service.SendAudioAsync(Context.Guild, Context.Channel, song);
        }

        //public virtual Task SetVolumeCmd([Range(1, 100)] int percentage)
        //{
        //    double v = percentage / 101d;
        //    _service.SetVolume(Context.Guild, v);
        //    return ReplyAsync($"Volume set to {percentage}%, will take effect when the next song starts.");
        //}

        [ClientInVoice]
        public virtual Task NextCmd()
        {
            _service.NextSong(Context.Guild);
            return Task.CompletedTask;
        }

        [ClientInVoice]
        public virtual Task StopCmd()
        {
            _service.StopPlaying(Context.Guild);
            return Task.CompletedTask;
        }
    }
}

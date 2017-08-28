using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
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
        public virtual Task JoinCmd(IVoiceChannel target = null)
        {
            target = target ?? (Context.User as IVoiceState).VoiceChannel;
            var self = (Context.Guild.CurrentUser);
            if (!self.HasPerms(target, AudioExt.DiscordPermissions.CONNECT))
            {
                return ReplyAsync("I can't connect to that channel.");
            }
            else if (!self.HasPerms(target, AudioExt.DiscordPermissions.SPEAK))
            {
                return ReplyAsync("I can't play in that channel.");
            }
            else
            {
                return _service.JoinAudio(Context.Guild, Context.Channel, target);
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

        [ClientInVoice]
        public virtual Task PauseCmd()
        {
            return _service.PausePlayback(Context.Guild);
            //return Task.CompletedTask;
        }

        [ClientInVoice]
        public virtual Task ResumeCmd()
        {
            return _service.ResumePlayback(Context.Guild);
            //return Task.CompletedTask;
        }

        [ClientInVoice]
        public virtual Task SetVolumeCmd([Range(1, 100)] int percentage)
        {
            float v = percentage / 101f;
            return _service.SetVolume(Context.Guild, v);
            //return ReplyAsync($"Volume set to {percentage}%.");
        }

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

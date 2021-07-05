using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Discord.Addons.SimpleAudio
{
    [RequireContext(ContextType.Guild)]
    public class AudioModule : ModuleBase<SocketCommandContext>
    {
        private readonly AudioService _service;

        protected AudioModule(AudioService service)
        {
            _service = service;
        }

        [ClientNotInVoice]
        public virtual Task JoinCmd([ValidateVoiceChannel] IVoiceChannel? target = null)
        {
            target ??= ((IVoiceState)Context.User).VoiceChannel;
            var channelPerms = Context.Guild.CurrentUser.GetPermissions(target);
            if (!channelPerms.Connect)
            {
                return ReplyAsync("I can't connect to that channel.");
            }
            else if (!channelPerms.Speak)
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
            _service.ListSongs(Context.Channel);
            return Task.CompletedTask;
        }

        [ClientInVoice]
        public virtual async Task PlayCmd([Remainder] string song)
        {
            await _service.SendAudio(Context.Guild, Context.Channel, song);
            await Context.Message.DeleteAsync();
        }

        [ClientInVoice]
        public virtual Task PlaylistCmd()
        {
            return _service.Playlist(Context.Guild);
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

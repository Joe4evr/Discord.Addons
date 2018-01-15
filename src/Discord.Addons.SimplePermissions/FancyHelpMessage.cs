using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;

namespace Discord.Addons.SimplePermissions
{
    internal sealed class FancyHelpMessage
    {
        internal const string SFirst  = "\u23EE";
        internal const string SBack   = "\u25C0";
        internal const string SNext   = "\u25B6";
        internal const string SLast   = "\u23ED";
        internal const string SDelete = "\u274C";

        private static IEmote EFirst  { get; } = new Emoji(SFirst);
        private static IEmote EBack   { get; } = new Emoji(SBack);
        private static IEmote ENext   { get; } = new Emoji(SNext);
        private static IEmote ELast   { get; } = new Emoji(SLast);
        private static IEmote EDelete { get; } = new Emoji(SDelete);

        private readonly IUser _user;
        private readonly IMessageChannel _channel;
        private readonly IEnumerable<CommandInfo> _commands;
        private readonly int _cmdsPerPage = 5;
        private readonly uint _totalPages;
        private readonly IApplication _app;

        internal ulong UserId => _user.Id;
        internal ulong MsgId => _msg.Id;
        private IUserMessage _msg;
        private uint _currentPage;

        public FancyHelpMessage(IMessageChannel channel, IUser user, IEnumerable<CommandInfo> commands, IApplication app)
        {
            _user = user;
            _channel = channel;
            _commands = commands;
            _currentPage = 0;
            _totalPages = (uint)Math.Ceiling((commands.Count() / (double)_cmdsPerPage));
            _app = app;
        }

        public async Task<FancyHelpMessage> SendMessage()
        {
            _msg = await _channel.SendMessageAsync("", embed: GetPage(0)).ConfigureAwait(false);
            await _msg.AddReactionAsync(EFirst).ConfigureAwait(false);
            await _msg.AddReactionAsync(EBack).ConfigureAwait(false);
            await _msg.AddReactionAsync(ENext).ConfigureAwait(false);
            await _msg.AddReactionAsync(ELast).ConfigureAwait(false);
            await _msg.AddReactionAsync(EDelete).ConfigureAwait(false);

            return this;
        }

        private Embed GetPage(int page)
        {
            var c = _commands.Skip(page * _cmdsPerPage).Take(_cmdsPerPage);
            //var m = c.First().Module.Name;
            return new EmbedBuilder()
                .WithAuthor(a => a.WithName(_app.Name)
                    .WithIconUrl(_app.IconUrl))
                .WithTitle("Available commands.")
                .WithDescription($"Page {page + 1} of {_totalPages}")
                .AddFieldSequence(c, (fb, cmd) => fb.WithIsInline(false)
                    .WithName($"{cmd.Module.Name}: {cmd.Aliases.FirstOrDefault()}")
                    .WithValue($"{cmd.Summary}\n\t{String.Join(", ", cmd.Parameters.Select(p => p.FormatParam()))}"))
                .WithFooter(fb => fb.WithText("Powered by SimplePermissions"))
                .Build();
        }

        public async Task First()
        {
            await _msg.RemoveReactionAsync(new Emoji(SFirst), _user);
            if (_currentPage == 0) return;

            await _msg.ModifyAsync(m => m.Embed = GetPage(0));

        }

        public async Task Next()
        {
            await _msg.RemoveReactionAsync(new Emoji(SNext), _user);
            if (_currentPage == (_totalPages - 1)) return;

            await _msg.ModifyAsync(m => m.Embed = GetPage((int)++_currentPage));
        }

        public async Task Back()
        {
            await _msg.RemoveReactionAsync(new Emoji(SBack), _user);
            if (_currentPage == 0) return;

            await _msg.ModifyAsync(m => m.Embed = GetPage((int)--_currentPage));
        }

        public async Task Last()
        {
            await _msg.RemoveReactionAsync(new Emoji(SLast), _user);
            if (_currentPage == (_totalPages - 1)) return;

            await _msg.ModifyAsync(m => m.Embed = GetPage((int)_totalPages - 1));
        }

        public Task Delete()
        {
            return _msg.DeleteAsync();
        }
    }
}

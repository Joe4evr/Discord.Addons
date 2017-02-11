using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using CommandParam = Discord.Commands.ParameterInfo;

namespace Discord.Addons.SimplePermissions
{
    internal sealed class FancyHelpMessage
    {
        internal const string EFirst = "⏪";
        internal const string EBack = "◀";
        internal const string ENext = "▶";
        internal const string ELast = "⏩";
        internal const string EDelete = "❌";

        private readonly IUser _user;
        private readonly IMessageChannel _channel;
        private readonly IEnumerable<CommandInfo> _commands;
        private readonly int _cmdsPerPage = 5;
        private readonly uint _totalPages;
        private readonly IApplication _app;

        internal ulong UserId => _user.Id;
        internal ulong MsgId => msg.Id;
        private IUserMessage msg;
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
            msg = await _channel.SendMessageAsync("", embed: GetPage(0));
            await msg.AddReactionAsync(EFirst);
            await Task.Delay(1000);
            await msg.AddReactionAsync(EBack);
            await Task.Delay(1000);
            await msg.AddReactionAsync(ENext);
            await Task.Delay(1000);
            await msg.AddReactionAsync(ELast);
            await Task.Delay(1000);
            await msg.AddReactionAsync(EDelete);

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
                    .WithValue($"{cmd.Summary}\n\t{String.Join(", ", cmd.Parameters.Select(p => formatParam(p)))}"))
                .WithFooter(fb => fb.WithText("Powered by SimplePermissions"))
                .Build();
        }

        private string formatParam(CommandParam param)
        {
            var sb = new StringBuilder();
            if (param.IsMultiple)
            {
                sb.Append($"`[({param.Type?.Name}): {param.Name}...]`");
            }
            else if (param.IsRemainder) //&& IsOptional - decided not to check for the combination
            {
                sb.Append($"`<({param.Type?.Name}): {param.Name}...>`");
            }
            else if (param.IsOptional)
            {
                sb.Append($"`[({param.Type?.Name}): {param.Name}]`");
            }
            else
            {
                sb.Append($"`<({param.Type?.Name}): {param.Name}>`");
            }

            if (!String.IsNullOrWhiteSpace(param.Summary))
            {
                sb.Append($" ({param.Summary})");
            }
            return sb.ToString();
        }

        public async Task First()
        {
            await msg.RemoveReactionAsync(EFirst, _user);
            if (_currentPage == 0) return;

            await msg.ModifyAsync(m => m.Embed = GetPage(0));

        }

        public async Task Next()
        {
            await msg.RemoveReactionAsync(ENext, _user);
            if (_currentPage == (_totalPages - 1)) return;

            await msg.ModifyAsync(m => m.Embed = GetPage((int)++_currentPage));
        }

        public async Task Back()
        {
            await msg.RemoveReactionAsync(EBack, _user);
            if (_currentPage == 0) return;

            await msg.ModifyAsync(m => m.Embed = GetPage((int)--_currentPage));
        }

        public async Task Last()
        {
            await msg.RemoveReactionAsync(ELast, _user);
            if (_currentPage == (_totalPages - 1)) return;

            await msg.ModifyAsync(m => m.Embed = GetPage((int)_totalPages - 1));
        }

        public Task Delete()
        {
            return msg.DeleteAsync();
        }
    }
}

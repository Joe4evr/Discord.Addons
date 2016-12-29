//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Commands;

//namespace Discord.Addons.SimplePermissions
//{
//    internal class FancyHelpMessage
//    {
//        //private static readonly string 

//        private readonly IMessageChannel _channel;
//        private readonly IUser _user;
//        private readonly IEnumerable<CommandInfo> _commands;


//        public FancyHelpMessage(IMessageChannel channel, IUser user, IEnumerable<CommandInfo> commands)
//        {
//            _channel = channel;
//            _user = user;
//            _commands = commands;
//        }

//        public Task<IUserMessage> SendMessage()
//        {
//            return _channel.SendMessageAsync("", embed: new EmbedBuilder()
//                );
//        }

//        public Task Forward()
//        {

//        }

//        public Task Back()
//        {

//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Text;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Addons.SimplePermissions;
//using Discord.Commands;
//using System.Threading.Tasks;
//using Discord;

//namespace Examples.SimplePermissions.EFProvider
//{

//    public class MyConfigUser : ConfigUser
//    {
//        public string Nickname { get; set; }
//    }

//    public class MyConfigChannel : ConfigChannel<MyConfigUser>
//    {
//        public string Topic { get; set; }
//    }

//    public class MyConfigGuild : ConfigGuild<MyConfigChannel, MyConfigUser>
//    {
//    }

//    public class MyEFConfig : EFBaseConfigContext<MyConfigGuild, MyConfigChannel, MyConfigUser>
//    {
//        public MyEFConfig(DbContextOptions options)
//            : base(options)
//        {
//        }

//        protected override Task OnUserAdd(MyConfigUser configUser, IGuildUser user)
//        {
//            configUser.Nickname = user.Nickname;
//            return Task.CompletedTask;
//        }

//        protected override Task OnChannelAdd(MyConfigChannel configChannel, ITextChannel channel)
//        {
//            configChannel.Topic = channel.Topic;
//            return Task.CompletedTask;
//        }

//        internal string GetLoginToken() => throw new NotImplementedException();
//    }

//    public class Factory : IDesignTimeDbContextFactory<MyEFConfig>
//    {
//        public MyEFConfig CreateDbContext(string[] args)
//        {
//            var map = new ServiceCollection()
//                .AddSingleton(new CommandService())
//                .AddDbContext<MyEFConfig>(opt => opt.UseSqlite(@"connection_string_here"))
//                .BuildServiceProvider();

//            return map.GetService<MyEFConfig>();
//        }
//    }
//}

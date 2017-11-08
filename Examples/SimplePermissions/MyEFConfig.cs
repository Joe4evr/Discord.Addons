using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.SimplePermissions;
using Discord.Commands;

namespace Examples.SimplePermissions
{
    public class MyUser : ConfigUser
    {
        public string Nickname { get; set; }
    }

    public class MyChannel : ConfigChannel<MyUser>
    {
        public string Topic { get; set; }
    }

    public class MyGuild : ConfigGuild<MyChannel, MyUser>
    {
        public string Admin { get; set; }
    }

    public class MyEFConfig : EFBaseConfigContext<MyGuild, MyChannel, MyUser>
    {
        public MyEFConfig(DbContextOptions options, CommandService commandService)
            : base(options, commandService)
        {
        }
    }

    public class Factory : IDesignTimeDbContextFactory<MyEFConfig>
    {
        public MyEFConfig CreateDbContext(string[] args)
        {
            var map = new ServiceCollection()
                .AddSingleton(new CommandService())
                .AddDbContext<MyEFConfig>(opt => opt.UseSqlite(@"Data Source=test.sqlite"))
                .BuildServiceProvider();

            return map.GetService<MyEFConfig>();
        }
    }
}

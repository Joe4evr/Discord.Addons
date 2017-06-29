using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    public abstract class BaseConfigStore<TConfig> : IConfigStore<TConfig>
        where TConfig : IPermissionConfig//, new()
    {
        protected CommandService Commands { get; }

        protected BaseConfigStore(CommandService commands)
        {
            Commands = commands;
        }

        public abstract TConfig Load();
    }
}

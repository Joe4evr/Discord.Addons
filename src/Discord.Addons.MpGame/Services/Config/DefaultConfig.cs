﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Addons.MpGame
{
    internal sealed class DefaultConfig : IMpGameServiceConfig
    {
        public static DefaultConfig Instance { get; } = new DefaultConfig();
        private DefaultConfig() { }

        ILogStrings IMpGameServiceConfig.LogStrings { get; } = DefaultLogStrings.Instance;
    }
}

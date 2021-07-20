//using System;
//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Discord.Commands;
//using Microsoft.Extensions.DependencyInjection;
//using System.Collections.Generic;

//namespace Discord.Addons.SimplePermissions
//{
//    /// <summary> </summary>
//    public sealed class EFCommandServiceExtension : IDbContextOptionsExtension
//    {
//        internal CommandService CommandService { get; }
//        DbContextOptionsExtensionInfo IDbContextOptionsExtension.Info => _info;

//        private readonly DbContextOptionsExtensionInfo _info;

//        /// <summary> </summary>
//        public EFCommandServiceExtension(CommandService commandService)
//        {
//            CommandService = commandService;
//            _info = new ExtensionInfo(this);
//        }

//        void IDbContextOptionsExtension.ApplyServices(IServiceCollection services) { }
//            //=> services.AddSingleton(CommandService);
//        void IDbContextOptionsExtension.Validate(IDbContextOptions options) { }

//        private sealed class ExtensionInfo : DbContextOptionsExtensionInfo
//        {
//            public override bool IsDatabaseProvider { get; } = false;
//            public override string LogFragment { get; } = String.Empty;

//            public ExtensionInfo(IDbContextOptionsExtension extension)
//                : base(extension)
//            {
//            }

//            public override long GetServiceProviderHashCode() => 0L;
//            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) { }
//        }
//    }
//}

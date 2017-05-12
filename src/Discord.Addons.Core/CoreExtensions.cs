using System;

namespace Discord.Commands
{
    internal static class CoreExtensions
    {
        internal static T GetService<T>(this IServiceProvider services)
            => (T)services.GetService(typeof(T));
    }
}

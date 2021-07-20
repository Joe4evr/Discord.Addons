using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Discord.Addons.SimplePermissions
{
    public static class Extensions
    {
        /// <summary>
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="services"></param>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddEFConfigContext<TContext>(
            this IServiceCollection services,
            Action<DbContextOptionsBuilder> optionsAction)
            where TContext : EFBaseConfigContext
        {
            services.AddDbContext<TContext>(optionsAction);

            return services;
        }
    }
}

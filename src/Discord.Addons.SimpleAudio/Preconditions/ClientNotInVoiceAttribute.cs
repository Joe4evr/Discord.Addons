using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace Discord.Addons.SimpleAudio
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    internal sealed class ClientNotInVoiceAttribute : AudioPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo _, IServiceProvider services)
        {
            var service = services.GetService<AudioService>();
            if (service != null)
            {
                if (CheckAllowCommands(service, context))
                {
                    return !service.Clients.ContainsKey(context.Guild.Id)
                        ? Task.FromResult(PreconditionResult.FromSuccess())
                        : Task.FromResult(PreconditionResult.FromError("This command can only be used when the client is not connected to voice."));
                }
                return Task.FromResult(PreconditionResult.FromError("Managing music via commands is disabled in this guild."));
            }

            return Task.FromResult(PreconditionResult.FromError("No AudioService found."));
        }
    }
}

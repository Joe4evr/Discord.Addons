using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace Discord.Addons.SimpleAudio
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    internal sealed class ClientInVoiceAttribute : AudioPreconditionAttribute
    {
        //private readonly RequireContextAttribute _ctx = new RequireContextAttribute(ContextType.Guild);

        public override async Task<PreconditionResult> CheckPermissionsAsync(
            ICommandContext context, CommandInfo _, IServiceProvider services)
        {
            var service = services.GetService<AudioService>();
            if (service != null)
            {
                if (await CheckAllowCommandsAsync(service, context))
                {
                    return service.Clients.ContainsKey(context.Guild.Id)
                        ? PreconditionResult.FromSuccess()
                        : PreconditionResult.FromError("This command can only be used when the client is connected to voice.");
                }
                return PreconditionResult.FromError("Managing music via commands is disabled in this guild.");
            }

            return PreconditionResult.FromError("No AudioService found.");
        }
    }
}

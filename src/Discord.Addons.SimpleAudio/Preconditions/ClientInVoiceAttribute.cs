using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;

namespace Discord.Addons.SimpleAudio
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    internal sealed class ClientInVoiceAttribute : PreconditionAttribute
    {
        //private readonly RequireContextAttribute _ctx = new RequireContextAttribute(ContextType.Guild);

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider map)
        {
            var service = map.GetService<AudioService>();
            if (service != null)
            {
                return service.Clients.ContainsKey(context.Guild.Id)
                    ? Task.FromResult(PreconditionResult.FromSuccess())
                    : Task.FromResult(PreconditionResult.FromError("This command can only be used when the client is connected to voice."));
            }

            return Task.FromResult(PreconditionResult.FromError("No AudioService found."));
        }
    }
}

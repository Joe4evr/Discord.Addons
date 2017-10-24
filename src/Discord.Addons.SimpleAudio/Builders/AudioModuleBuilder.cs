//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Discord.Commands;
//using Discord.Commands.Builders;

//namespace Discord.Addons.SimpleAudio
//{
//    public sealed class AudioModuleBuilder
//    {
//        private readonly CommandService _commands;

//        internal AudioModuleBuilder(CommandService commands)
//        {
//            _commands = commands;

//            //Module = commands.CreateModuleBuilder(null);
//            //JoinCommand = Module.CreateCommandBuilder()
//            //    .WithName("join")
//            //    .WithRunMode(RunMode.Async)
//            //    .WithSummary("Connect to a voice channel")
//            //    .AddPrecondition(new ClientNotInVoiceAttribute())
//            //    .AddParameter<IVoiceChannel>("channel", param =>
//            //    {
//            //        param.IsOptional = true;
//            //    });

//            Module.AddCommand("join",
//                async (context, parameters, provider, command) =>
//                {
//                    var svc = provider.GetService<AudioService>();
//                    var target = (parameters[0] as IVoiceChannel) ?? (context.User as IVoiceState).VoiceChannel;
//                    var self = await context.Guild.GetCurrentUserAsync();

//                    if (!self.HasPerms(target, AudioExt.DiscordPermissions.CONNECT))
//                    {
//                        await context.Channel.SendMessageAsync("I can't connect to that channel.");
//                    }
//                    else if (!self.HasPerms(target, AudioExt.DiscordPermissions.SPEAK))
//                    {
//                        await context.Channel.SendMessageAsync("I can't play in that channel.");
//                    }
//                    else
//                    {
//                        await svc.JoinAudio(context.Guild, context.Channel, target);
//                    }
//                },
//                (builder) => { });
//        }

//        public ModuleBuilder Module { get; }

//        public CommandBuilder JoinCommand { get; }

//        public Task Build()
//        {
//            foreach (var cmd in Module.Commands)
//            {
//                if (cmd.RunMode != RunMode.Async)
//                {
//                    throw new InvalidOperationException("Audio-related commands should always be Async. Do not change the RunMode setting.");
//                }
//            }

//            //return _commands.CreateModuleAsync(null, Md)
//            return Task.CompletedTask;
//        }
//    }
//}

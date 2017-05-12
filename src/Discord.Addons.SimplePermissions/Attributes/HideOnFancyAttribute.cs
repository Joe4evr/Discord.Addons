//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Discord.Commands;

//namespace Discord.Addons.SimplePermissions
//{
//    internal class HideOnFancyAttribute : PreconditionAttribute
//    {
//        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
//        {
//            if (map.TryGet<PermissionsService>(out var svc))
//            {
//                if (svc.Helpmsgs.TryGetValue(context.Message.Id, out var fhm))
//                {
//                    //return !(await config.GetFancyHelpValue(context.Guild))
//                    //    ? PreconditionResult.FromSuccess()
//                    //    : PreconditionResult.FromError("FancyHelp is on.");
//                }
//            }
//            else
//            {
//                return PreconditionResult.FromError("PermissionService not found.");
//            }
//        }
//    }
//}

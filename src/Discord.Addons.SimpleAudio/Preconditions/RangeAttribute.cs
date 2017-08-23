using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimpleAudio
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class RangeAttribute : ParameterPreconditionAttribute
    {
        private readonly int _min;
        private readonly int _max;

        public RangeAttribute(int min, int max)
        {
            _min = min;
            _max = max;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider map)
        {
            if (value is int v && (v < _min || v > _max))
            {
                return Task.FromResult(PreconditionResult.FromError($"Parameter value must be between {_min} and {_max}."));
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
        }
    }
}

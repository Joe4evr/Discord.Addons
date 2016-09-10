using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Sets how often a user is allowed to use this command.
    /// </summary>
    /// <remarks>This is backed by an in-memory collection
    /// and will not persist with restarts.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RatelimitAttribute : PreconditionAttribute
    {
        private uint InvokeLimit { get; }
        private TimeSpan InvokeLimitPeriod { get; }
        private Dictionary<ulong, Timeout> InvokeTracker { get; } = new Dictionary<ulong, Timeout>();

        /// <summary>
        /// Sets how often a user is allowed to use this command.
        /// </summary>
        /// <param name="times">The number of times a user may use the command within a certain period.</param>
        /// <param name="period">The amount of time since first invoke a user has until the limit is lifted.</param>
        /// <param name="measure">The scale in which the 'period' parameter should be measuered.</param>
        public RatelimitAttribute(uint times, double period, RateMeasure measure)
        {
            InvokeLimit = times;
            switch (measure)
            {
                case RateMeasure.Days: InvokeLimitPeriod = TimeSpan.FromDays(period);
                    break;
                case RateMeasure.Hours: InvokeLimitPeriod = TimeSpan.FromHours(period);
                    break;
                case RateMeasure.Minutes: InvokeLimitPeriod = TimeSpan.FromMinutes(period);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="executingCommand"></param>
        /// <param name="moduleInstance"></param>
        /// <returns></returns>
        public override Task<PreconditionResult> CheckPermissions(IUserMessage context, Command executingCommand, object moduleInstance)
        {
            var now = DateTime.UtcNow;
            Timeout timeout;

            if (!InvokeTracker.TryGetValue(context.Author.Id, out timeout) ||
                ((now - timeout.FirstInvoke) > InvokeLimitPeriod))
            {
                timeout = new Timeout(now);
            }

            timeout.TimesInvoked++;

            if (timeout.TimesInvoked <= InvokeLimit)
            {
                InvokeTracker[context.Author.Id] = timeout;
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("You are currently in Timeout."));
            }
        }

        private class Timeout
        {
            public uint TimesInvoked { get; set; }
            public DateTime FirstInvoke { get; }

            public Timeout(DateTime timeStarted)
            {
                FirstInvoke = timeStarted;
            }
        }
    }

    /// <summary>
    /// Sets the scale of the period parameter.
    /// </summary>
    public enum RateMeasure
    {
        /// <summary>
        /// Period is measured in days.
        /// </summary>
        Days,

        /// <summary>
        /// Period is measured in hours.
        /// </summary>
        Hours,

        /// <summary>
        /// Period is measured in minutes.
        /// </summary>
        Minutes
    }
}

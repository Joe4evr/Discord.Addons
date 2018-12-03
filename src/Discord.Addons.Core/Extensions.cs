using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord;

namespace Discord.Addons.Core
{
    internal static class Extensions
    {
        public static Func<LogMessage, Task> NoOpLogger { get; } = (_ => Task.CompletedTask);
        public static Func<string, Task> NoOpStringToTask { get; } = (_ => Task.CompletedTask);
        public static Func<string, ValueTask> NoOpStringToVTask { get; } = (_ => new ValueTask(Task.CompletedTask));

        internal static string ToNiceString(this TimeSpan ts)
        {
            var d = ts.TotalDays == 1 ? "day" : "days";
            var h = ts.Hours == 1 ? "hour" : "hours";
            var m = ts.Minutes == 1 ? "minute" : "minutes";

            return (ts.TotalHours > 24)
                ? $"{ts.Days} {d} and {ts.Hours} {h}"
                : $"{ts.Hours} {h} and {ts.Minutes} {m}";
        }

        //Method for randomizing lists using a Fisher-Yates shuffle.
        //Based on http://stackoverflow.com/questions/273313/
        /// <summary>
        ///     Perform a Fisher-Yates shuffle on a collection implementing <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="source">
        ///     The list to shuffle.
        /// </param>
        /// <param name="iterations">
        ///     The amount of iterations you wish to perform.
        /// </param>
        /// <remarks>
        ///     Adapted from http://stackoverflow.com/questions/273313/.
        /// </remarks>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, int iterations = 1)
        {
            var provider = RandomNumberGenerator.Create();
            var buffer = source.ToList();
            int n = buffer.Count;
            for (int i = 0; i < iterations; i++)
            {
                while (n > 1)
                {
                    byte[] box = new byte[(n / Byte.MaxValue) + 1];
                    int boxSum;
                    do
                    {
                        provider.GetBytes(box);
                        boxSum = box.Sum(b => b);
                    }
                    while (!(boxSum < n * ((Byte.MaxValue * box.Length) / n)));
                    int k = (boxSum % n);
                    n--;
                    T value = buffer[k];
                    buffer[k] = buffer[n];
                    buffer[n] = value;
                }
            }

            return buffer;
        }
    }
}

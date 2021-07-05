using System;
using System.Threading.Tasks;

namespace Discord.Addons.Core
{
    internal static class TaskExtensions
    {
        // Task

        public static Task OnCancellation<TState>(
            this Task task, Action<Task, TState> cancelCallback, TState state)
        {
            return task.ContinueWith(static (t, s) =>
            {
                var (cb, st) = ((Action<Task, TState>, TState))s!;
                cb(t, st);
            }, (cancelCallback, state), TaskContinuationOptions.OnlyOnCanceled);
        }
    }
}

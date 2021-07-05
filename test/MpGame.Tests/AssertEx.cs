using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MpGame.Tests
{
    internal static class AssertEx
    {
        public static void DoesNotRaise<T>(
            Action<EventHandler<T>> attach,
            Action<EventHandler<T>> detach,
            Action testCode)
            where T : EventArgs
        {
            Assert.RaisedEvent<T> raised = null;
            void handler(object s, T args) => raised = new Assert.RaisedEvent<T>(s, args);

            attach(handler);
            testCode();
            detach(handler);
            Assert.Null(raised);
        }

        public static async Task DoesNotRaiseAsync<T>(
            Action<EventHandler<T>> attach,
            Action<EventHandler<T>> detach,
            Func<Task> testCode)
            where T : EventArgs
        {
            Assert.RaisedEvent<T> raised = null;
            void handler(object s, T args) => raised = new Assert.RaisedEvent<T>(s, args);

            attach(handler);
            await testCode();
            detach(handler);
            Assert.Null(raised);
        }
    }
}

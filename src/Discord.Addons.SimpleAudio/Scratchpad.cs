using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Scratchpad
{
    //    public class CommandService<TContext>
    //        where TContext : class, ICommandContext
    //    {
    //        public void AddModule<T>() where T : ModuleBase<TContext>
    //        {
    //        }
    //    }

    //    public static class C
    //    {
    //        static void M()
    //        {
    //            var svc = new CommandService<ICommandContext>();
    //            //svc.AddModule<UniversalContextModule>();
    //            //svc.AddModule<SpecificContextModule>();
    //        }
    //    }

    //    public interface ICommandContext
    //    {
    //    }

    //    //internal interface IModuleBase
    //    //{
    //    //    void SetContext(ICommandContext ctx);
    //    //}

    //    //public interface IModuleBase<out T>
    //    //    where T : ICommandContext
    //    //{
    //    //    //T Context { get; }
    //    //}

    //    public abstract class ModuleBase<T> //: IFoo, IFoo<T>
    //        where T : class, ICommandContext
    //    {
    //        public T Context { get; private set; }

    //        internal void SetContext(ICommandContext ctx)
    //        {
    //            var x = ctx as T;
    //            if (x == null)
    //            {
    //                throw new InvalidOperationException($"Invalid type. Expected {typeof(T).Name}, got {ctx.GetType().Name}");
    //            }

    //            Context = x;
    //        }
    //    }

    //    public class SpecificContext : ICommandContext
    //    {
    //    }

    //    public class UniversalContextModule : ModuleBase<ICommandContext>
    //    {
    //    }

    //    public class SpecificContextModule : ModuleBase<SpecificContext>
    //    {
    //    }
}

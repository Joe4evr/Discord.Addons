using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace Discord.Addons.SimplePermissions
{
    /// <summary> Instructs the <see cref="PermissionsModule"/>'s help command to not
    /// display this particular command or overload. This is a marker attribute. </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class HiddenAttribute : Attribute
    {
    }
}

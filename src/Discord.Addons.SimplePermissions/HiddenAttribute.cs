using System;

namespace Discord.Addons.SimplePermissions
{
    /// <summary>
    /// Instructs the <see cref="PermissionsModule"/>'s help command to not
    /// display this particular command or overload. This is a marker attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HiddenAttribute : Attribute
    {
    }
}

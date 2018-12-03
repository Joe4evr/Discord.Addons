namespace Discord.Addons.Core
{
    internal struct Unit
    {
        public override bool Equals(object obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public override string ToString() => "()";
        public static bool operator ==(Unit left, Unit right) => true;
        public static bool operator !=(Unit left, Unit right) => false;
    }

    internal static class UnitEx
    {
        /// <summary>
        ///     Return a specified value chained after a <see cref="Unit"/> returning method.
        /// </summary>
        public static T ReturnWith<T>(this Unit _, T value) => value;
    }
}

namespace Cthangover.Mods.Interface.Shell
{
    using System;
    /// <summary>One idempotent input/pause claim; leaving the shell revokes every outstanding lease.</summary>
    public sealed class ShellLease : IDisposable
    {
        private ShellSession owner;
        private readonly long id;
        internal ShellLease(ShellSession session, long key) { owner = session; id = key; }
        public void Dispose() { var session = owner; owner = null; if (Godot.GodotObject.IsInstanceValid(session)) session.Release(id); }
    }
}

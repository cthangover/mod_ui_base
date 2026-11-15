namespace Cthangover.Mods.Interface.Shell
{
    using Godot;
    /// <summary>Optional shell content (for example dialogs) attaches before readiness. The assembly consumer removes contributors on unload.</summary>
    public interface IShellContributor { void Attach(Node root); }
}

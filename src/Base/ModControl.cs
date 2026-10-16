using Godot;

namespace Cthangover.Mods.Interface.Base
{
    /// <summary>
    /// Lifecycle forwarder for Control scripts compiled from mods — the Control
    /// counterpart of <see cref="ModNode"/>. Godot's C# source generator only
    /// runs in the main game assembly, so engine callbacks overridden inside a
    /// runtime-compiled mod are never registered with the engine. Mod controls
    /// derive from this base (which has bindings) and override the OnXxx methods.
    /// </summary>
    public partial class ModControl : Control
    {
        /// <summary>Engine callback; forwards to <see cref="OnEnterTree"/>.</summary>
        public override void _EnterTree() => OnEnterTree();

        /// <summary>Engine callback; forwards to <see cref="OnExitTree"/>.</summary>
        public override void _ExitTree() => OnExitTree();

        /// <summary>Engine callback; forwards to <see cref="OnReady"/>.</summary>
        public override void _Ready() => OnReady();

        /// <summary>Engine callback; forwards to <see cref="OnProcess"/>.</summary>
        public override void _Process(double delta) => OnProcess(delta);

        /// <summary>Forwarded from <see cref="Node._EnterTree"/>. Override in mod controls.</summary>
        protected virtual void OnEnterTree() { }

        /// <summary>Forwarded from <see cref="Node._ExitTree"/>. Override in mod controls.</summary>
        protected virtual void OnExitTree() { }

        /// <summary>Forwarded from <see cref="Node._Ready"/>. Override in mod controls.</summary>
        protected virtual void OnReady() { }

        /// <summary>Forwarded from <see cref="Node._Process"/>. Override in mod controls.</summary>
        protected virtual void OnProcess(double delta) { }
    }
}
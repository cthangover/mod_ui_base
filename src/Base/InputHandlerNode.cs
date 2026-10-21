using Godot;

namespace Cthangover.Mods.Interface.Base
{
    /// <summary>
    /// Forwarder pattern for Godot input: proxies _Input to a virtual OnInput
    /// so derived classes can handle input events without needing to know about
    /// Godot's _Input override mechanics.
    /// </summary>
    public partial class InputHandlerNode : Node
    {
        /// <summary>Engine entry point: forwards the input event to <see cref="OnInput"/>.</summary>
        public override void _Input(InputEvent @event)
        {
            OnInput(@event);
        }

        /// <summary>Override in derived classes to receive Godot input events. Called by <see cref="_Input"/> which proxies from the engine.</summary>
        protected virtual void OnInput(InputEvent @event) { }
    }
}

namespace Cthangover.Mods.Interface.View
{
    using System;
    using System.Collections.Generic;
    using Godot;
    /// <summary>Owned capture lease. Async/native-window callers hide before their draw fence and dispose after reading the window.</summary>
    public sealed class ViewportCaptureSuppression : IDisposable
    {
        private List<CanvasLayer> hidden;
        internal ViewportCaptureSuppression(List<CanvasLayer> hidden)=>this.hidden=hidden;
        public void Dispose(){if(hidden==null)return;foreach(var layer in hidden)if(GodotObject.IsInstanceValid(layer))layer.Show();hidden=null;}
    }
}

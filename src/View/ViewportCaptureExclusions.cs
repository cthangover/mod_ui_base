namespace Cthangover.Mods.Interface.View
{
    using System.Collections.Generic;
    using Cthangover.Core.Utils;
    using Godot;
    /// <summary>Borrowed diagnostic canvases only. Normal game HUD is never registered here. Captures restore each layer's previous visibility even after decode/save failure.</summary>
    public static class ViewportCaptureExclusions
    {
        private static readonly List<CanvasLayer> layers=new();
        public static void Register(CanvasLayer layer){if(!layers.Contains(layer))layers.Add(layer);}
        public static void Unregister(CanvasLayer layer)=>layers.Remove(layer);
        public static ViewportCaptureSuppression Hide(Viewport viewport)
        {
            if(!GameEnvironment.IsRuntime)return null;
            List<CanvasLayer> hidden=null;
            foreach(var layer in layers)if(GodotObject.IsInstanceValid(layer)&&layer.IsInsideTree()&&layer.GetViewport()==viewport&&layer.Visible)
            {hidden??=new();hidden.Add(layer);layer.Hide();}
            return hidden==null?null:new ViewportCaptureSuppression(hidden);
        }
    }
}

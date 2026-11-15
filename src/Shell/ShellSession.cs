namespace Cthangover.Mods.Interface.Shell
{
    using System;
    using System.Collections.Generic;
    using Cthangover.Core.Bootstrap;
    using Cthangover.Core.Events;
    using Cthangover.Core.Scenes;
    using Cthangover.Core.UI;
    using Cthangover.Core.UI.Dialog;
    using Godot;

    /// <summary>Common 2D/3D shell readiness and input ownership. Pauses only the supplied simulation subtree; menus/UI and loading continue processing.</summary>
    public partial class ShellSession : Node
    {
        internal static readonly List<IShellContributor> Contributors = new();
        private readonly Dictionary<long, ShellBlock> claims = new();
        private long sequence;
        private Input.MouseModeEnum previousMouse;
        private Node simulation;
        private bool announced;
        [Export] public string Kind { get; set; } = "2d";
        [Export] public bool CapturePointer { get; set; }
        public static ShellSession Current { get; private set; }
        public bool Ready { get; private set; }
        public bool InputAllowed { get; private set; }
        public bool SimulationPaused { get; private set; }
        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always; Current = this;
            previousMouse = Input.MouseMode;
            GetParent().SetMeta("shell_kind", Kind);
            Callable.From(Activate).CallDeferred();
        }
        private void Activate()
        {
            if (!IsInsideTree() || announced || ApplicationExit.IsQuitting) return;
            announced = true;
            SceneContextNode.Instance?.ClearData();
            foreach (var contributor in Contributors.ToArray()) contributor.Attach(GetParent());
            Ready = true;
            ModEventBus.Publish(new ShellReadyEvent { Root = GetParent() });
            Refresh();
            var manager = GetNodeOrNull<SceneManager>("/root/SceneManager");
            if (!string.IsNullOrEmpty(manager?.PendingSceneName))
            {
                var target = manager.PendingSceneName; manager.PendingSceneName = null; manager.SwitchScene(target);
            }
        }
        /// <summary>Installs the location's simulation owner; never pauses the whole SceneTree.</summary>
        public void SetSimulation(Node root) { simulation = root; Refresh(); }
        public ShellLease Acquire(ShellBlock block)
        {
            var id = ++sequence; claims[id] = block; Refresh(); return new ShellLease(this, id);
        }
        internal void Release(long id) { claims.Remove(id); Refresh(); }
        /// <summary>Can be refreshed synchronously before input dispatch as well as each frame.</summary>
        public void Refresh()
        {
            if (!IsInsideTree()) return;
            var blocked = ShellBlock.None;
            foreach (var claim in claims.Values) blocked |= claim;
            if (ModalVisible()) blocked |= ShellBlock.All;
            if (!GetWindow().HasFocus()) blocked |= ShellBlock.Input | ShellBlock.Pointer;
            SimulationPaused = (blocked & ShellBlock.Simulation) != 0 || GetTree().Paused;
            InputAllowed = Ready && !ApplicationExit.IsQuitting && !SimulationPaused && (blocked & ShellBlock.Input) == 0;
            if (GodotObject.IsInstanceValid(simulation)) simulation.ProcessMode = SimulationPaused ? ProcessModeEnum.Disabled : ProcessModeEnum.Pausable;
            if (CapturePointer) Input.MouseMode = InputAllowed && (blocked & ShellBlock.Pointer) == 0 ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
        }
        private bool ModalVisible()
        {
            foreach (var panel in AppServices.Current.Ui.AllPanels)
                if (UiHost.Instance != null && UiHost.Instance.TryGetInstance(panel.Id, out var control) && control.IsVisibleInTree()) return true;
            if (GetParent().FindChild("DialogBox", true, false) is IDialogBox dialog && dialog.IsRunning) return true;
            foreach (var window in GetViewport().GetEmbeddedSubwindows()) if (window.Visible) return true;
            return false;
        }
        public override void _Process(double delta) => Refresh();
        public override void _ExitTree()
        {
            Ready = false; claims.Clear();
            if (Current == this) { Current = null; if (CapturePointer) Input.MouseMode = previousMouse; }
            simulation = null;
        }
    }
}

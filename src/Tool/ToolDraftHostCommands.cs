namespace Cthangover.Mods.Interface.Tool
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Cthangover.Core.Mods.HotReload;
    using Cthangover.Core.UI.Tool;
    using Godot;

    /// <summary>Host-controlled capture/restore works outside the replaced editor assembly and returns every unmapped value.</summary>
    public sealed class ToolDraftHostCommands : IModDevelopmentCommand
    {
        public IReadOnlyCollection<string> Commands { get; } = new[] { "tool.open", "tool.draft.capture", "tool.draft.suspend", "tool.draft.restore", "tool.draft.reopen", "tool.draft.inspect" };
        public string Description => "Capture named tool fields and document assets in temporary cache; restore by stable IDs, returning unmapped IDs, original values and reasons. Requires path; restore/inspect require capture_id.";
        public async Task<JsonNode> ExecuteAsync(ModDevelopmentContext context, string command, CancellationToken token)
        {
            var id = context.Request["capture_id"]?.GetValue<string>();
            if (command == "tool.draft.inspect")
                return ToolDraftCache.Inspect(id);

            if (command is "tool.open" or "tool.draft.reopen")
            {
                var tool = context.Request["tool"]?.GetValue<string>() ?? (id == null ? null : ToolDraftCache.Inspect(id)["tool_id"]?.GetValue<string>());
                var provider = ToolFactory.Instance.Get(tool) ?? throw new ArgumentException("Tool provider not found: " + tool);
                var opened = ToolWindow.ShowWindow(provider.CreateWindow()); provider = null;
                var openedPath = opened.GetPath().ToString();
                if (command == "tool.open") { opened = null; context.MarkChanged(); return new JsonObject { ["path"] = openedPath, ["tool"] = tool }; }
                var restoring = ToolDraftCache.RestoreAsync(opened, id, token); opened = null;
                var restored = await restoring; restored["path"] = openedPath;
                ToolDraftRefreshCover.Hide(context.Tree.Root, id); context.MarkChanged(); return restored;
            }
            var path = context.Request["path"]?.GetValue<string>() ?? throw new ArgumentException("Tool window path is required");
            var window = context.Tree.Root.GetNodeOrNull<Window>(path) ?? throw new ArgumentException("Tool window not found: " + path);
            if (command is "tool.draft.capture" or "tool.draft.suspend")
            {
                var captured = ToolDraftCache.Capture(window, context.Request["tool"]?.GetValue<string>());
                if (command == "tool.draft.suspend")
                {
                    ToolDraftRefreshCover.Show(window, captured["capture_id"].GetValue<string>()); window.QueueFree(); context.MarkChanged();
                }
                window = null; return captured;
            }
            var pending = ToolDraftCache.RestoreAsync(window, id, token); window = null;
            var result = await pending; context.MarkChanged(); return result;
        }
    }
}

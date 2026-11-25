namespace Cthangover.Mods.Interface.Tool
{
    using System;
    using System.IO;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Godot;

    /// <summary>Temporary disk-backed drafts survive collectible assembly unloads and process replacement. No editor object enters the cache.</summary>
    public static class ToolDraftCache
    {
        private static string DirectoryFor(string captureId) => Path.Combine(Path.GetTempPath(), "cthangover-tool-drafts", Guid.Parse(captureId).ToString("N"));

        public static JsonObject Capture(Window window, string toolId = null)
        {
            var id = Guid.NewGuid().ToString("N"); var folder = DirectoryFor(id); Directory.CreateDirectory(folder);
            var fields = ToolDraftFields.Capture(window);
            var state = new JsonObject { ["format"] = "cth.tool-draft.v1", ["capture_id"] = id, ["window_type"] = window.GetType().Name, ["tool_id"] = toolId,
                ["fields"] = fields, ["document"] = window is IToolDraftState adapter ? adapter.CaptureToolDraft(folder) : null };
            File.WriteAllText(Path.Combine(folder, "state.json"), state.ToJsonString());
            return new JsonObject { ["capture_id"] = id, ["field_count"] = fields.Count, ["file"] = Path.Combine(folder, "state.json") };
        }

        public static JsonObject Inspect(string id)
        {
            var path = Path.Combine(DirectoryFor(id), "state.json");
            if (new FileInfo(path).Length > 8388608) throw new IOException("Tool draft exceeds 8 MiB");
            return JsonNode.Parse(File.ReadAllText(path)).AsObject();
        }

        private static void AddLoss(JsonArray losses, JsonNode item)
        {
            foreach (var existing in losses)
                if (existing["id"]?.GetValue<string>() == item["id"]?.GetValue<string>() && existing["reason"]?.GetValue<string>() == item["reason"]?.GetValue<string>()) return;
            losses.Add(item.DeepClone());
        }

        public static async Task<JsonObject> RestoreAsync(Window target, string id, CancellationToken cancellation = default)
        {
            var state = Inspect(id); JsonObject result;
            if (state["document"] is JsonObject document && target is IToolDraftState adapter)
            {
                result = await adapter.RestoreToolDraftAsync(document, DirectoryFor(id), cancellation);
                adapter = null;
                if (result["applied"]?.GetValue<bool>() == true && result["handled_fields"] is JsonArray handled)
                {
                    var remaining = new JsonArray();
                    foreach (var field in state["fields"].AsArray())
                    {
                        var key = field["id"].GetValue<string>(); var skip = false;
                        foreach (var name in handled) if (name.GetValue<string>() == key) skip = true;
                        if (result["handled_prefixes"] is JsonArray prefixes)
                            foreach (var prefix in prefixes) if (key.StartsWith(prefix.GetValue<string>(), StringComparison.Ordinal)) skip = true;
                        if (!skip) remaining.Add(field.DeepClone());
                    }
                    foreach (var item in ToolDraftFields.Apply(target, remaining)) AddLoss(result["unmapped"].AsArray(), item);
                }
                var missing = ToolDraftFields.Unmapped(target, state["fields"].AsArray());
                foreach (var item in missing) AddLoss(result["unmapped"].AsArray(), item);
            }
            else
            {
                var lost = ToolDraftFields.Apply(target, state["fields"].AsArray());
                if (state["document"] != null) lost.Add(ToolDraftFields.Lost("document", state["document"], "document_adapter_missing"));
                result = new JsonObject { ["applied"] = true, ["unmapped"] = lost };
            }
            target = null;
            result["capture_id"] = id; result["mapping_complete"] = result["unmapped"].AsArray().Count == 0;
            return result;
        }
    }
}

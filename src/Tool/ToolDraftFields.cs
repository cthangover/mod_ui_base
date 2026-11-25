namespace Cthangover.Mods.Interface.Tool
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Nodes;
    using Godot;

    /// <summary>Stable named controls form the fallback migration contract. Unnamed layout nodes are deliberately excluded.</summary>
    public static class ToolDraftFields
    {
        public static JsonArray Capture(Node window)
        {
            var result = new JsonArray();
            foreach (var child in window.FindChildren("*", "", true, false))
            {
                var name = child.Name.ToString(); if (name.StartsWith('@') || InFileDialog(child, window)) continue;
                JsonNode value = child switch
                {
                    SpinBox spin => JsonValue.Create(spin.Value), TextEdit text => JsonValue.Create(text.Text),
                    LineEdit line => JsonValue.Create(line.Text),
                    OptionButton option => new JsonObject { ["index"] = option.Selected, ["text"] = option.Selected >= 0 ? option.GetItemText(option.Selected) : "" },
                    BaseButton button when button.ToggleMode => JsonValue.Create(button.ButtonPressed), _ => null
                };
                if (value != null) result.Add(new JsonObject { ["id"] = name, ["type"] = child.GetType().Name, ["value"] = value });
            }
            return result;
        }

        private static bool InFileDialog(Node child, Node window)
        {
            for (var parent = child.GetParent(); parent != null && parent != window; parent = parent.GetParent())
                if (parent is FileDialog) return true;
            return false;
        }

        /// <summary>Returns missing/ambiguous/type-changed controls with their original values, even when a document adapter handles restoration.</summary>
        public static JsonArray Unmapped(Node target, JsonArray fields)
        {
            var lost = new JsonArray();
            foreach (var field in fields)
            {
                var matches = target.FindChildren(field["id"].GetValue<string>(), "", true, false);
                var reason = matches.Count == 0 ? "missing_control" : matches.Count > 1 ? "ambiguous_control" : matches[0].GetType().Name != field["type"].GetValue<string>() ? "control_type_changed" : null;
                if (reason == null && matches[0] is OptionButton option)
                {
                    var found = field["value"]["index"].GetValue<int>() < 0 && option.ItemCount == 0; for (var i = 0; i < option.ItemCount; i++) if (option.GetItemText(i) == field["value"]["text"].GetValue<string>()) found = true;
                    if (!found) reason = "choice_removed";
                }
                if (reason == null && matches[0] is SpinBox spin && (field["value"].GetValue<double>() < spin.MinValue || field["value"].GetValue<double>() > spin.MaxValue)) reason = "value_outside_new_range";
                if (reason != null) lost.Add(Lost(field["id"].GetValue<string>(), field["value"], reason));
            }
            return lost;
        }

        internal static JsonArray Apply(Node target, JsonArray fields)
        {
            var lost = Unmapped(target, fields); var blocked = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in lost) blocked.Add(item["id"].GetValue<string>());
            foreach (var field in fields)
            {
                var id = field["id"].GetValue<string>(); if (blocked.Contains(id)) continue;
                var child = target.FindChildren(id, "", true, false)[0]; var value = field["value"];
                try
                {
                    switch (child)
                    {
                        case SpinBox spin:
                            spin.Value = value.GetValue<double>();
                            if (Math.Abs(spin.Value - value.GetValue<double>()) > .000001) lost.Add(Lost(id, value, "value_clamped_or_rounded"));
                            break;
                        case TextEdit text: text.Text = value.GetValue<string>(); break;
                        case LineEdit line: line.Text = value.GetValue<string>(); break;
                        case OptionButton option:
                            if (value["index"].GetValue<int>() < 0 && option.ItemCount == 0) break;
                            var index = -1; for (var i = 0; i < option.ItemCount; i++) if (option.GetItemText(i) == value["text"].GetValue<string>()) index = i;
                            if (index < 0) lost.Add(Lost(id, value, "choice_removed"));
                            else { option.Select(index); option.EmitSignal(OptionButton.SignalName.ItemSelected, index); }
                            break;
                        case BaseButton button when button.ToggleMode: button.ButtonPressed = value.GetValue<bool>(); break;
                    }
                }
                catch (Exception error) { lost.Add(Lost(id, value, error.Message)); }
            }
            return lost;
        }

        public static JsonObject Lost(string id, JsonNode value, string reason) =>
            new() { ["id"] = id, ["value"] = value?.DeepClone(), ["reason"] = reason };

        /// <summary>Detects document members ignored by a newer schema; reports values rather than silently dropping them.</summary>
        public static void FindLostDocumentValues(JsonNode oldValue, JsonNode restored, string path, JsonArray lost)
        {
            if (oldValue is JsonObject oldObject && restored is JsonObject newObject)
                foreach (var pair in oldObject)
                {
                    var id = path + "." + pair.Key;
                    if (!newObject.ContainsKey(pair.Key)) lost.Add(Lost(id, pair.Value, "document_field_removed"));
                    else FindLostDocumentValues(pair.Value, newObject[pair.Key], id, lost);
                }
            else if (oldValue is JsonArray oldArray && restored is JsonArray newArray)
                for (var i = 0; i < oldArray.Count; i++)
                    if (i >= newArray.Count) lost.Add(Lost(path + "[" + i + "]", oldArray[i], "document_array_entry_removed"));
                    else FindLostDocumentValues(oldArray[i], newArray[i], path + "[" + i + "]", lost);
            else if (!JsonNode.DeepEquals(oldValue, restored))
            {
                var item = Lost(path, oldValue, "document_value_changed"); item["replacement_value"] = restored?.DeepClone(); lost.Add(item);
            }
        }
    }
}

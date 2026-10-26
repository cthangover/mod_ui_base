using System.Collections.Generic;
using Cthangover.Core.UI.Theming;
using Godot;

namespace Cthangover.Mods.Interface.Theming
{
    /// <summary>
    /// Shared source palettes for editable fields and read-only previews.
    /// Each call returns a new highlighter: Godot binds it to one TextEdit and
    /// keeps a per-document cache, so sharing a resource between editors would
    /// mix their text. Scenario commands are supplied by the caller to keep
    /// the UI toolkit independent of the dialog engine.
    /// </summary>
    public static class SourceSyntax
    {
        /// <summary>Colours JSON strings, numbers, punctuation and literal values, including unfinished input.</summary>
        public static CodeHighlighter CreateJsonHighlighter()
        {
            var highlighter = CreateBase();
            highlighter.AddColorRegion("\"", "\"", UiTokens.AccentBright);
            return highlighter;
        }

        /// <summary>Colours Godot text scene/resource sections, strings, constructors, numbers and comments.</summary>
        public static CodeHighlighter CreateTscnHighlighter()
        {
            var highlighter = CreateBase();
            highlighter.FunctionColor = UiTokens.TypeTarget;
            highlighter.AddColorRegion("\"", "\"", UiTokens.AccentBright);
            highlighter.AddColorRegion(";", "", UiTokens.Faint, true);
            foreach (var section in new[] { "gd_scene", "gd_resource", "ext_resource", "sub_resource", "resource", "node", "connection", "editable" })
                highlighter.AddKeywordColor(section, UiTokens.TypeTarget);
            return highlighter;
        }

        /// <summary>Colours scenario commands, named arguments, quoted dialogue, labels and comments.</summary>
        public static CodeHighlighter CreateScenarioHighlighter(IEnumerable<string> commands)
        {
            var highlighter = CreateBase();
            if (commands != null)
            {
                foreach (var command in commands)
                    highlighter.AddKeywordColor(command, UiTokens.Info);
            }
            highlighter.AddKeywordColor("select", UiTokens.Info);
            highlighter.AddKeywordColor("option", UiTokens.Info);
            foreach (var argument in new[] {
                "key", "first", "second", "hide_color", "wait", "speed", "hidden",
                "sprites", "loop", "target", "quest_id", "show", "hide", "duration"
            })
                highlighter.AddKeywordColor(argument, UiTokens.Accent);
            highlighter.AddColorRegion("\"", "\"", UiTokens.AccentBright);
            highlighter.AddColorRegion("#", "", UiTokens.Faint, true);
            highlighter.AddColorRegion(":", " ", UiTokens.TypeTarget, true);
            return highlighter;
        }

        private static CodeHighlighter CreateBase()
        {
            var highlighter = new CodeHighlighter
            {
                NumberColor = UiTokens.Info,
                SymbolColor = UiTokens.Muted,
                FunctionColor = UiTokens.Body,
                MemberVariableColor = UiTokens.Body,
            };
            foreach (var literal in new[] { "true", "false", "null" })
                highlighter.AddKeywordColor(literal, UiTokens.TypeTarget);
            return highlighter;
        }
    }
}

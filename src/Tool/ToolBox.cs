using System.Collections.Generic;
using Cthangover.Core.Bootstrap;
using Cthangover.Core.Factories.Impls;
using Cthangover.Core.Mods;
using Cthangover.Core.Scenes;
using Cthangover.Core.UI;
using Cthangover.Core.UI.Theming;
using Cthangover.Core.Utils;
using Godot;

using Cthangover.Mods.Interface.Theming;
using Cthangover.Core.UI.Tool;
namespace Cthangover.Mods.Interface.Tool
{
    /// <summary>
    /// Main toolbar/HUD container (the <c>TopView</c> shell node). It no longer
    /// knows any concrete panel: buttons for mod-provided panels are handed to it
    /// by <see cref="UiBuilder"/> from <see cref="UiRegistry.PanelEntries"/>, and
    /// pressing one calls <see cref="UiRegistry.Toggle"/>. Tool buttons
    /// (<see cref="IToolBoxButton"/>/<see cref="ToolFactory"/>) remain a separate,
    /// self-registering mechanism and are added alongside the panel buttons.
    /// </summary>
    public partial class ToolBox : Control, IToolbarHost, ISaveIconHost
    {
        private HBoxContainer _buttons;
        private TextureRect _saveIcon;

        /// <summary>Wires the save icon visibility and appends the dev-tool buttons registered in <see cref="ToolBoxButtonFactory"/>.</summary>
        public override void _Ready()
        {
            _buttons = GetNodeOrNull<HBoxContainer>("Buttons");
            _saveIcon = GetNodeOrNull<TextureRect>("CenterTools/Save");
            UpdateSaveIconVisibility();

            AddToolButtons(_buttons);
        }

        /// <summary>
        /// Materializes a toolbar button for every entry. Called once per shell by
        /// <see cref="UiBuilder"/>; entries arrive pre-sorted by the registry
        /// (button index, then mod load order, then id).
        /// </summary>
        public void ApplyPanelButtons(IReadOnlyList<UiEntry> entries)
        {
            if (_buttons == null || entries == null)
                return;

            foreach (var entry in entries)
                _buttons.AddChild(CreatePanelButton(entry));
        }

        private static BaseButton CreatePanelButton(UiEntry entry)
        {
            var title = TranslationServer.Translate(entry.Button.Locale);
            var icon = UIIconFactory.Instance.Get(entry.Button.Icon);

            var button = CreateToolbarButton(title, icon);
            var id = entry.Id;
            button.Pressed += () => AppServices.Current.Ui.ToggleEntry(id);
            return button;
        }

        private static BaseButton CreateToolbarButton(string title, Texture2D icon)
        {
            var button = new Button
            {
                CustomMinimumSize = new Vector2(60, 60),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                TooltipText = title,
            };

            if (icon != null)
            {
                var iconRect = UiKit.Icon(icon, 60);
                button.AddChild(iconRect);
                iconRect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            }
            else
            {
                button.Text = title;
            }

            button.AddThemeStyleboxOverride("normal", UiKit.Frame(UiTokens.RadiusSm, UiTokens.Surface0, UiTokens.Line));
            button.AddThemeStyleboxOverride("hover", UiKit.Frame(UiTokens.RadiusSm, UiTokens.Surface1, UiTokens.AccentLine));
            button.AddThemeStyleboxOverride("pressed", UiKit.Frame(UiTokens.RadiusSm, UiTokens.Surface1, UiTokens.AccentLineStrong));
            button.AddThemeStyleboxOverride("disabled", UiKit.Frame(UiTokens.RadiusSm, UiTokens.Surface0, UiTokens.LineDisabled));
            button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

            return button;
        }

        /// <summary>
        /// Adds one "tools" toolbar button whose popup lists every visible
        /// <see cref="IToolBoxButton"/>. Tools used to get a text button each,
        /// which did not fit the 60×60 icon row and overlapped its icons; a
        /// single icon keeps the HUD row uniform no matter how many tools are
        /// installed. Nothing is added when no tool button is visible.
        /// </summary>
        private void AddToolButtons(HBoxContainer container)
        {
            if (container == null)
                return;

            var visible = new List<IToolBoxButton>(ToolBoxButtonFactory.Instance.GetVisible());
            if (visible.Count == 0)
                return;

            visible.Sort((a, b) => string.Compare(
                TranslationServer.Translate(a.LocaleKey), TranslationServer.Translate(b.LocaleKey),
                System.StringComparison.CurrentCulture));

            var btn = CreateToolbarButton(TranslationServer.Translate("tools/menu_title"), UIIconFactory.Instance.Get("tools"));
            var menu = new PopupMenu { Theme = ToolTheme.Get() };
            foreach (var buttonDef in visible)
                menu.AddItem(TranslationServer.Translate(buttonDef.LocaleKey));
            menu.IndexPressed += index => OpenTool(visible[(int)index].ToolId);
            btn.AddChild(menu);
            btn.Pressed += () =>
            {
                var rect = btn.GetGlobalRect();
                menu.Position = new Vector2I((int)rect.Position.X, (int)rect.End.Y + UiTokens.Space1);
                menu.Popup();
            };
            container.AddChild(btn);
        }

        private static void OpenTool(string toolId)
        {
            var tool = ToolFactory.Instance.Get(toolId);
            if (tool != null)
                ToolWindow.ShowWindow(tool.CreateWindow());
        }

        /// <summary>
        /// Shows or hides the save icon based on <see cref="SceneManager.IsSaveAllowedForCurrentScene"/>.
        /// Call after scene transitions to reflect the new scene's save policy.
        /// </summary>
        public void UpdateSaveIconVisibility()
        {
            if (_saveIcon == null)
                return;
            _saveIcon.Visible = SceneManager.IsSaveAllowedForCurrentScene();
        }
    }
}

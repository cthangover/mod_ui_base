using System.Collections.Generic;
using Cthangover.Core.Bootstrap;
using Cthangover.Core.Factories.Impls;
using Cthangover.Core.Scenes;
using Cthangover.Core.UI.Theming;
using Cthangover.Core.Utils;
using Godot;
using Cthangover.Core.UI.Menu;
using Cthangover.Mods.Interface.Base.Lists;

namespace Cthangover.Mods.Interface.Menu
{
    /// <summary>
    /// Materializes the buttons collected in the <see cref="MenuRegistry"/> into
    /// the containers of a host menu. The host supplies a section → container
    /// map (""game"", ""mods"", ""exit"", ""tools"", …); every registry button is
    /// added to the container of its section, in registry order, and carries
    /// its translation key in node metadata (<see cref="LocaleMetaKey"/>) so the
    /// host can retranslate it on language change.
    /// <para/>
    /// A button is bound either to a scene path (pressed → the scene opens as a
    /// fullscreen child of the menu root and owns its lifetime) or to a handler
    /// delegate (pressed → the delegate runs). Unknown sections fall back to the
    /// ""mods"" container; sections without a container are skipped loudly.
    /// </summary>
    public static class MenuBuilder
    {
        /// <summary>Node metadata key holding the button's translation key.</summary>
        public const string LocaleMetaKey = "main_menu_locale";

        /// <summary>
        /// Creates one button per registry entry inside the section containers.
        /// The host menu is passed in because pressed handlers instantiate mod
        /// scenes as its children.
        /// </summary>
        public static void Build(Control menuRoot, IReadOnlyDictionary<string, Container> sections)
        {
            if (menuRoot == null || sections == null)
                return;

            foreach (var entry in AppServices.Current.Menus.Buttons)
            {
                if (entry.Visible != null && !SafeVisible(entry))
                    continue;

                if (!sections.TryGetValue(entry.Section ?? MenuButtonEntry.SectionMods, out var container)
                    && !sections.TryGetValue(MenuButtonEntry.SectionMods, out container))
                {
                    GameLogger.Log("MENU_BUILDER", $"No container for section '{entry.Section}' (button '{entry.Id}')");
                    continue;
                }

                if (!GodotObject.IsInstanceValid(container))
                {
                    GameLogger.Log("MENU_BUILDER", $"Container for section '{entry.Section}' is gone (button '{entry.Id}')");
                    continue;
                }

                container.AddChild(CreateButton(menuRoot, entry));
            }
        }

        /// <summary>
        /// Collects buttons created by <see cref="Build"/> (or by tools) that
        /// carry a locale meta key into the host's translation map, so the host
        /// keeps them localized across language switches.
        /// </summary>
        private static bool SafeVisible(MenuButtonEntry entry)
        {
            try
            {
                return entry.Visible();
            }
            catch (System.Exception ex)
            {
                GameLogger.Log("MENU_BUILDER", $"Visibility check of button '{entry.Id}' failed: {ex.Message}", LogLevel.Warning);
                return true;
            }
        }

        public static void CollectLocaleKeys(Container container, Dictionary<Button, string> keys)
        {
            if (container == null || keys == null)
                return;

            foreach (var child in container.GetChildren())
            {
                if (child is Button button && button.HasMeta(LocaleMetaKey))
                    keys[button] = button.GetMeta(LocaleMetaKey).AsString();
            }
        }

        /// <summary>Largest icon width of a menu button, so an entry icon never makes its row taller than the text rows.</summary>
        private const int MenuIconSize = 22;

        private static Button CreateButton(Control menuRoot, MenuButtonEntry entry)
        {
            var title = TranslationServer.Translate(entry.Locale);
            var button = new Button
            {
                Name = entry.Id,
                Text = title,
                TooltipText = title,
            };

            if (!string.IsNullOrEmpty(entry.Icon))
            {
                var icon = UIIconFactory.Instance.Get(entry.Icon);
                if (icon != null)
                {
                    // Entry icons are toolbar icons (60 px); in a menu row they stay text-sized.
                    button.Icon = icon;
                    button.AddThemeConstantOverride("icon_max_width", MenuIconSize);
                }
            }

            if (!string.IsNullOrEmpty(entry.Variation))
                button.ThemeTypeVariation = "Cthangover" + char.ToUpper(entry.Variation[0]) + entry.Variation[1..];

            button.SetMeta(LocaleMetaKey, entry.Locale);
            button.Pressed += () => Invoke(menuRoot, entry);
            return button;
        }

        private static void Invoke(Control menuRoot, MenuButtonEntry entry)
        {
            if (entry.Handler != null)
            {
                GameLogger.Log("MENU_BUILDER", $"Pressed '{entry.Id}' (handler)");
                entry.Handler(menuRoot);
                return;
            }

            OpenScene(menuRoot, entry.ScenePath, entry.Id);
        }

        /// <summary>
        /// Opens a mod scene as a fullscreen child of the menu root: the scene
        /// owns its lifetime and closes itself, or is replaced by another menu
        /// scene opening over it.
        /// </summary>
        /// <summary>Opens a mod scene as a full-rect overlay of the menu and returns it (null when it fails to build).</summary>
        public static Node OpenScene(Control menuRoot, string scenePath, string label = null)
        {
            var node = new ModScenePrefab(scenePath).Instantiate();
            if (node == null)
            {
                GameLogger.Log("MENU_BUILDER", $"Open '{label ?? scenePath}': scene '{scenePath}' failed to build", LogLevel.Error);
                return null;
            }

            menuRoot.AddChild(node);
            CthangoverTheme.Apply(node);

            if (node is Control control)
                control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

            if (node is ModWidget modWidget)
                modWidget.EnsureConstructed();

            GameLogger.Log("MENU_BUILDER", $"Opened '{label ?? scenePath}' → {scenePath}");
            return node;
        }
    }
}

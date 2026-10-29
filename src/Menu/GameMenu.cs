using System.Collections.Generic;
using Cthangover.Core.Scenes;
using Godot;
using Cthangover.Core.UI.Menu;

namespace Cthangover.Mods.Interface.Menu
{
    /// <summary>
    /// In-game pause menu: save/load/settings/quit. The settings and save/load
    /// panels are not referenced by type: they are registered in the
    /// <see cref="MenuRegistry"/> (by the <c>menu_base</c> mod or any other mod
    /// that wants to provide them) and instantiated here by id, so the panel
    /// implementations can live entirely inside mods. Both panels stay hidden
    /// children created on _Ready to avoid loading delays when the player opens
    /// the menu. RefreshSaveButton dynamically disables save if the current
    /// scene doesn't allow saves (checked via
    /// SceneManager.IsSaveAllowedForCurrentScene). Button text is translated
    /// with a refresh-on-language-change subscription.
    /// </summary>
    public partial class GameMenu : Control
    {
        private ISettingsPanel _settingsPanel;
        private ISaveLoadPanel _saveLoadPanel;
        private Button _saveBtn;
        private Label _title;
        private readonly Dictionary<Button, string> _btnKeys = new();

        /// <summary>Wires menu buttons, instantiates the hidden settings and save/load panels from the <see cref="MenuRegistry"/>, and subscribes to language changes.</summary>
        public override void _Ready()
        {
            _saveBtn = GetNode<Button>("Panel/Margin/VBox/SaveBtn");
            _saveBtn.Pressed += OnSaveClick;

            GetNode<Button>("Panel/Margin/VBox/ReturnBtn").Pressed += OnReturnClick;
            GetNode<Button>("Panel/Margin/VBox/LoadBtn").Pressed += OnLoadClick;
            GetNode<Button>("Panel/Margin/VBox/SettingsBtn").Pressed += OnSettingsClick;
            GetNode<Button>("Panel/Margin/VBox/MainMenuBtn").Pressed += OnMainMenuClick;
            GetNode<Button>("Panel/Margin/VBox/ExitBtn").Pressed += OnExitClick;

            _btnKeys[GetNode<Button>("Panel/Margin/VBox/ReturnBtn")] = "menu/resume";
            _btnKeys[_saveBtn] = "menu/save";
            _btnKeys[GetNode<Button>("Panel/Margin/VBox/LoadBtn")] = "menu/load";
            _btnKeys[GetNode<Button>("Panel/Margin/VBox/SettingsBtn")] = "menu/settings";
            _btnKeys[GetNode<Button>("Panel/Margin/VBox/MainMenuBtn")] = "menu/main_menu";
            _btnKeys[GetNode<Button>("Panel/Margin/VBox/ExitBtn")] = "menu/exit";
            _title = GetNode<Label>("Panel/Margin/VBox/Title");

            AttachPanel(MenuRegistry.OpenPanel("settings"));
            AttachPanel(MenuRegistry.OpenPanel("save_load"));

            Visible = false;
            RefreshButtons();
            VisibilityChanged += OnVisibilityChanged;
        }

        /// <summary>
        /// Adopts a registry-built panel as a hidden child and forwards its
        /// typed behaviour to the menu handlers. Panels registered without a
        /// matching interface still work as plain overlays.
        /// </summary>
        private void AttachPanel(Node panel)
        {
            if (panel == null)
                return;

            if (panel is Control control)
                control.Visible = false;
            AddChild(panel);

            switch (panel)
            {
                case ISettingsPanel settings:
                    settings.LanguageChanged += RefreshButtons;
                    _settingsPanel = settings;
                    break;
                case ISaveLoadPanel saveLoad:
                    _saveLoadPanel = saveLoad;
                    break;
            }
        }

        /// <summary>
        /// Refreshes the save button whenever the menu becomes visible. The panel is
        /// opened through <see cref="UI.UiRegistry.Toggle"/>, so this signal replaces
        /// the old toolbar callback that used to do the refresh before showing.
        /// </summary>
        private void OnVisibilityChanged()
        {
            if (Visible)
                RefreshSaveButton();
        }

        private void RefreshButtons()
        {
            foreach (var kv in _btnKeys)
                kv.Key.Text = TranslationServer.Translate(kv.Value);
            _title.Text = TranslationServer.Translate("menu/title");
        }

        private void OnReturnClick()
        {
            Visible = false;
        }

        private void OnSaveClick()
        {
            _saveLoadPanel?.OpenForSave();
        }

        private bool IsSaveAllowed()
        {
            return SceneManager.IsSaveAllowedForCurrentScene();
        }

        /// <summary>
        /// Enables or disables the save button based on whether the current scene permits saving.
        /// Called by <see cref="ToolBox.OnSettingsClick"/> before showing the game menu, so the
        /// button state reflects the current scene's save policy.
        /// </summary>
        public void RefreshSaveButton()
        {
            _saveBtn.Disabled = !IsSaveAllowed();
        }

        private void OnLoadClick()
        {
            _saveLoadPanel?.OpenForLoad();
        }

        private void OnSettingsClick()
        {
            _settingsPanel?.Open();
        }

        private void OnMainMenuClick()
        {
            var sceneService = SceneContextNode.FindNode<GodotSceneService>("GodotSceneService");
            sceneService?.SwitchToMenu();
        }

        private void OnExitClick()
        {
            GetTree().Quit();
        }
    }
}

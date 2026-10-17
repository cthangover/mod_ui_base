using System;
using System.Collections.Generic;
using Cthangover.Core.UI.Theming;
using Cthangover.Core.Utils;
using Godot;
using Cthangover.Core.UI.Tool;

namespace Cthangover.Mods.Interface.Tool
{
    /// <summary>
    /// Base for developer tool windows: borderless chrome that follows the UI
    /// style guide (title left in the header, ✕ on the right, drag-to-move,
    /// double-click/□ to maximize, edge resize), a status bar, dirty-state
    /// tracking and keyboard shortcuts.
    /// <para/>
    /// Subclasses build their UI in the constructor and add it as direct
    /// children; <see cref="SetupChrome"/> (run by <see cref="Open{T}"/> and
    /// <see cref="ShowWindow"/>) later moves those children into the content
    /// area between the header and the status bar. Status and hint text may be
    /// set before the chrome exists — it is buffered and shown once built.
    /// <para/>
    /// Tool windows use <see cref="ToolTheme"/>, a denser copy of the game
    /// theme: same tokens, smaller paddings, so editor toolbars fit.
    /// <para/>
    /// Shortcuts: Ctrl+S calls <see cref="OnSaveShortcut"/> even while a text
    /// field has focus (checked in <c>_Input</c>, before the GUI); every other
    /// key reaches <see cref="HandleKey"/> only if no focused control consumed
    /// it (<c>_UnhandledInput</c>), so Delete in a text field never deletes a
    /// selected object.
    /// </summary>
    public abstract partial class ToolWindow : Window
    {
        /// <summary>
        /// Whether the window has unsaved changes. When <c>true</c>, the title bar shows a "*" prefix
        /// and closing asks for confirmation first.
        /// </summary>
        protected bool _dirty;
        private readonly string _baseTitle;

        private bool _chromeBuilt;

        private bool _isMaximized;
        private Vector2I _restorePosition;
        private Vector2I _restoreSize;

        private Label _titleLabel;
        private Button _maximizeBtn;
        private HBoxContainer _titleBar;
        private Label _statusLabel;
        private Label _hintLabel;
        private string _pendingStatus = string.Empty;
        private ToolStatusKind _pendingStatusKind;
        private string _pendingHint = string.Empty;

        private bool _draggingTitle;
        private Vector2 _titleDragMouseStart;
        private Vector2I _titleDragWindowStart;

        private enum ResizeEdge { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }
        private bool _resizing;
        private ResizeEdge _resizeEdge;
        private Vector2 _resizeMouseStart;
        private Vector2I _resizePosStart;
        private Vector2I _resizeSizeStart;

        private const int EDGE_ZONE = 6;
        private const int CORNER_ZONE = 14;
        private const int TITLE_BAR_HEIGHT = 40;
        private const int TITLE_BUTTON_SIZE = 38;
        private const float DefaultSizeRatio = 0.9f;

        /// <summary>
        /// Height of the in-game HUD icon row. HUD canvas layers draw above
        /// embedded windows, so a window placed under it would have its title
        /// bar covered; windows open below this line.
        /// </summary>
        private const int HudTopInset = 84;

        private static Font _monospaceFont;

        private static Vector2 GetGameViewportSize()
        {
            var tree = Godot.Engine.GetMainLoop() as SceneTree;
            var vpSize = tree?.Root?.GetVisibleRect().Size;
            if (vpSize.HasValue && vpSize.Value.X > 0 && vpSize.Value.Y > 0)
                return vpSize.Value;

            return DisplayServer.WindowGetSize();
        }

        // ── public API ──────────────────────────────────────────────

        /// <summary>
        /// Creates a new instance of <typeparamref name="T"/> and displays it as a centered popup
        /// on the scene tree root. Returns the window so the caller can inspect it after closing.
        /// </summary>
        public static T Open<T>() where T : ToolWindow, new()
        {
            var window = new T();
            ShowWindow(window);
            return window;
        }

        /// <summary>
        /// Displays an already-constructed <see cref="Window"/> as a centered popup on the scene
        /// tree root. Used when the window was created via <see cref="IToolProvider.CreateWindow"/>.
        /// </summary>
        public static Window ShowWindow(Window window)
        {
            if (window is ToolWindow tw)
                tw.SetupChrome();
            else
                CthangoverTheme.Apply(window);
            var tree = Godot.Engine.GetMainLoop() as SceneTree;
            tree?.Root.AddChild(window);
            window.PopupCentered();
            if (window is ToolWindow && window.Position.Y < HudTopInset)
                window.Position = new Vector2I(window.Position.X, HudTopInset);
            GameLogger.Log("TOOL_WINDOW", $"ShowWindow '{window.GetType().Name}' size=({window.Size.X},{window.Size.Y})");
            return window;
        }

        // ── constructor & lifecycle ─────────────────────────────────

        protected ToolWindow(string titleKey)
        {
            _baseTitle = TranslationServer.Translate(titleKey);
            Title = _baseTitle;
            Unresizable = false;
            CloseRequested += RequestClose;
        }

        /// <summary>Runs the <see cref="Cleanup"/> teardown hook when the window leaves the tree.</summary>
        public override void _ExitTree()
        {
            Cleanup();
        }

        /// <summary>Ctrl+S → <see cref="OnSaveShortcut"/>, checked before the GUI so it works inside text fields.</summary>
        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey key || !key.Pressed || key.Echo)
                return;

            if (key.CtrlPressed && key.Keycode == Key.S)
            {
                OnSaveShortcut();
                SetInputAsHandled();
            }
        }

        /// <summary>Forwards keys no control consumed to <see cref="HandleKey"/>.</summary>
        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey key && key.Pressed && HandleKey(key))
                SetInputAsHandled();
        }

        /// <summary>
        /// Override to release resources when the window is closed. Called from <c>_ExitTree</c>.
        /// </summary>
        protected virtual void Cleanup() { }

        /// <summary>Ctrl+S handler; tools with a save action override it.</summary>
        protected virtual void OnSaveShortcut() { }

        /// <summary>
        /// Tool-specific shortcut for a key no focused control consumed.
        /// Return <c>true</c> when handled.
        /// </summary>
        protected virtual bool HandleKey(InputEventKey key) => false;

        // ── chrome setup ────────────────────────────────────────────

        /// <summary>
        /// Wraps all currently-added children inside the chrome layout (title bar,
        /// content area, status bar, resize zones). Called automatically by
        /// <see cref="Open{T}"/> and <see cref="ShowWindow"/>; safe to call twice.
        /// </summary>
        internal void SetupChrome()
        {
            if (_chromeBuilt)
                return;
            _chromeBuilt = true;

            Theme = ToolTheme.Get();

            // Read the requested size first: assigning MinSize clamps Size at once.
            var requested = Size;
            var viewport = GetGameViewportSize();
            Borderless = true;
            MinSize = new Vector2I(800, 400);
            MaxSize = (Vector2I)viewport;

            if (requested.X < MinSize.X || requested.Y < MinSize.Y)
                requested = (Vector2I)(viewport * DefaultSizeRatio);
            var maxHeight = MaxSize.Y - HudTopInset - UiTokens.Space3;
            Size = new Vector2I(Mathf.Min(requested.X, MaxSize.X), Mathf.Min(requested.Y, maxHeight));

            var existingChildren = new List<Node>();
            foreach (Node child in GetChildren())
                existingChildren.Add(child);
            foreach (var child in existingChildren)
                RemoveChild(child);

            var rootPanel = BuildRootPanel();

            var rootVBox = new VBoxContainer();
            rootVBox.AddThemeConstantOverride("separation", 0);
            rootPanel.AddChild(rootVBox);

            rootVBox.AddChild(BuildTitleBar());

            var contentMargin = new MarginContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            contentMargin.AddThemeConstantOverride("margin_left", UiTokens.Space3);
            contentMargin.AddThemeConstantOverride("margin_right", UiTokens.Space3);
            contentMargin.AddThemeConstantOverride("margin_top", UiTokens.Space3);
            contentMargin.AddThemeConstantOverride("margin_bottom", UiTokens.Space1);
            rootVBox.AddChild(contentMargin);

            var contentArea = new Control
            {
                Name = "ContentArea",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            contentMargin.AddChild(contentArea);

            foreach (var child in existingChildren)
                contentArea.AddChild(child);

            rootVBox.AddChild(BuildStatusBar());

            AddResizeZones();
        }

        // ── dirty tracking ──────────────────────────────────────────

        /// <summary>
        /// Marks the window as having unsaved changes. Prepends "* " to the title bar text.
        /// </summary>
        protected void SetDirty()
        {
            if (_dirty)
                return;

            _dirty = true;
            UpdateTitleText();
        }

        /// <summary>
        /// Clears the dirty flag and restores the original title bar text.
        /// </summary>
        protected void MarkClean()
        {
            _dirty = false;
            UpdateTitleText();
        }

        private void UpdateTitleText()
        {
            var text = _dirty ? "* " + _baseTitle : _baseTitle;
            Title = text;
            if (_titleLabel != null)
                _titleLabel.Text = text;
        }

        // ── status bar ──────────────────────────────────────────────

        /// <summary>Shows a message in the status bar, coloured by <paramref name="kind"/>.</summary>
        protected void SetStatus(string text, ToolStatusKind kind = ToolStatusKind.Info)
        {
            _pendingStatus = text ?? string.Empty;
            _pendingStatusKind = kind;
            if (_statusLabel == null)
                return;

            _statusLabel.Text = _pendingStatus;
            _statusLabel.AddThemeColorOverride("font_color", kind switch
            {
                ToolStatusKind.Ok => UiTokens.Ok,
                ToolStatusKind.Error => UiTokens.DangerText,
                _ => UiTokens.Body,
            });
        }

        /// <summary>Sets the faint right-aligned hint of the status bar (usually the shortcut list).</summary>
        protected void SetHint(string text)
        {
            _pendingHint = text ?? string.Empty;
            if (_hintLabel != null)
                _hintLabel.Text = _pendingHint;
        }

        // ── unsaved-changes dialogs ─────────────────────────────────

        /// <summary>Asks "close without saving?" and runs <paramref name="onDiscard"/> on confirmation.</summary>
        protected void ShowUnsavedDialog(Action onDiscard)
        {
            ShowConfirm("tools/unsaved_title", "tools/unsaved_text", onDiscard);
        }

        /// <summary>
        /// Runs <paramref name="action"/> right away when there are no unsaved
        /// changes, otherwise only after the author agrees to discard them; on
        /// refusal <paramref name="onCancel"/> runs (e.g. to roll a file picker
        /// back). Use before replacing the edited document.
        /// </summary>
        protected void RunDiscardingChanges(Action action, Action onCancel = null)
        {
            if (!_dirty)
            {
                action();
                return;
            }

            ShowConfirm("tools/unsaved_title", "tools/unsaved_discard_text", () =>
            {
                MarkClean();
                action();
            }, onCancel);
        }

        /// <summary>Shows a themed OK/Cancel dialog; <paramref name="onConfirm"/> runs on OK, <paramref name="onCancel"/> otherwise.</summary>
        protected void ShowConfirm(string titleKey, string textKey, Action onConfirm, Action onCancel = null)
        {
            var dialog = new ConfirmationDialog
            {
                Title = TranslationServer.Translate(titleKey),
                DialogText = TranslationServer.Translate(textKey),
                Theme = ToolTheme.Get()
            };
            dialog.Confirmed += () => { dialog.QueueFree(); onConfirm(); };
            dialog.Canceled += () => { dialog.QueueFree(); onCancel?.Invoke(); };
            AddChild(dialog);
            dialog.PopupCentered();
        }

        /// <summary>Closes the window, asking first when there are unsaved changes.</summary>
        protected void RequestClose()
        {
            if (_dirty)
                ShowUnsavedDialog(QueueFree);
            else
                QueueFree();
        }

        // ── layout helpers ──────────────────────────────────────────

        protected static VBoxContainer CreateFillContainer()
        {
            var c = new VBoxContainer();
            c.AnchorRight = 1f;
            c.AnchorBottom = 1f;
            c.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            c.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            c.AddThemeConstantOverride("separation", UiTokens.Space3);
            return c;
        }

        protected static HBoxContainer CreateToolbar()
        {
            var t = new HBoxContainer();
            t.AddThemeConstantOverride("separation", UiTokens.Space2);
            return t;
        }

        protected static PanelContainer CreateSidebar(VBoxContainer content, int width = 280)
        {
            var panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(width, 100);
            panel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            panel.AddThemeStyleboxOverride("panel", SidebarBox());

            var scroll = new ScrollContainer();
            scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
            scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            scroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            panel.AddChild(scroll);
            content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            content.AddThemeConstantOverride("separation", UiTokens.Space2);
            scroll.AddChild(content);

            return panel;
        }

        /// <summary>Inset sidebar surface: <c>--ct-surface-2</c>, line border, card radius.</summary>
        protected static StyleBoxFlat SidebarBox()
        {
            var style = new StyleBoxFlat
            {
                BgColor = UiTokens.Surface2,
                BorderColor = UiTokens.Line,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                BorderWidthTop = 1,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = UiTokens.RadiusMd,
                CornerRadiusTopRight = UiTokens.RadiusMd,
                CornerRadiusBottomLeft = UiTokens.RadiusMd,
                CornerRadiusBottomRight = UiTokens.RadiusMd,
                ContentMarginLeft = UiTokens.Space4,
                ContentMarginRight = UiTokens.Space4,
                ContentMarginTop = UiTokens.Space4,
                ContentMarginBottom = UiTokens.Space4
            };
            return style;
        }

        /// <summary>
        /// Monospace font for code, IDs and numbers (style guide: <c>--ct-mono</c>).
        /// The game ships no mono font, so the OS one is requested by name with the
        /// guide's fallbacks; Godot falls back to its default font if none exist.
        /// </summary>
        public static Font GetMonospaceFont()
        {
            return _monospaceFont ??= new SystemFont
            {
                FontNames = new[] { "Consolas", "Cascadia Mono", "DejaVu Sans Mono", "Liberation Mono", "Courier New", "monospace" }
            };
        }

        // ── chrome building ─────────────────────────────────────────

        private PanelContainer BuildRootPanel()
        {
            var panel = new PanelContainer
            {
                Name = "RootPanel",
                AnchorRight = 1f,
                AnchorBottom = 1f
            };

            var style = new StyleBoxFlat
            {
                BgColor = UiTokens.Surface0,
                BorderWidthLeft = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                BorderWidthTop = 1,
                BorderColor = UiTokens.Line,
                CornerRadiusTopLeft = UiTokens.RadiusLg,
                CornerRadiusTopRight = UiTokens.RadiusLg,
                CornerRadiusBottomLeft = UiTokens.RadiusLg,
                CornerRadiusBottomRight = UiTokens.RadiusLg,
                ContentMarginLeft = 1,
                ContentMarginRight = 1,
                ContentMarginBottom = 1,
                ContentMarginTop = 1
            };
            panel.AddThemeStyleboxOverride("panel", style);
            AddChild(panel);

            return panel;
        }

        private PanelContainer BuildTitleBar()
        {
            _titleBar = new HBoxContainer
            {
                Name = "TitleBar",
                CustomMinimumSize = new Vector2(0, TITLE_BAR_HEIGHT)
            };
            _titleBar.AddThemeConstantOverride("separation", UiTokens.Space1);

            _titleLabel = new Label
            {
                Text = _dirty ? "* " + _baseTitle : _baseTitle,
                Uppercase = true,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            _titleLabel.AddThemeColorOverride("font_color", UiTokens.InkBright);
            _titleLabel.AddThemeFontSizeOverride("font_size", UiTokens.FontLead);
            _titleBar.AddChild(_titleLabel);

            _maximizeBtn = CreateTitleButton("□", OnMaximizeToggle, UiTokens.BtnHover, UiTokens.InkBright);
            _maximizeBtn.TooltipText = TranslationServer.Translate("tools/window_maximize");
            _titleBar.AddChild(_maximizeBtn);

            var closeBtn = CreateTitleButton("✕", RequestClose, UiTokens.DangerSoft, UiTokens.DangerText);
            closeBtn.TooltipText = TranslationServer.Translate("tools/window_close");
            _titleBar.AddChild(closeBtn);

            _titleBar.GuiInput += OnTitleBarInput;

            var titlePanel = new PanelContainer();
            titlePanel.AddChild(_titleBar);

            var titleBg = new StyleBoxFlat
            {
                BgColor = UiTokens.Surface1,
                BorderColor = UiTokens.Line,
                BorderWidthBottom = 1,
                CornerRadiusTopLeft = UiTokens.RadiusLg,
                CornerRadiusTopRight = UiTokens.RadiusLg,
                ContentMarginLeft = UiTokens.Space7,
                ContentMarginRight = UiTokens.Space1,
            };
            titlePanel.AddThemeStyleboxOverride("panel", titleBg);

            return titlePanel;
        }

        private PanelContainer BuildStatusBar()
        {
            var bar = new HBoxContainer { Name = "StatusBar", CustomMinimumSize = new Vector2(0, 26) };
            bar.AddThemeConstantOverride("separation", UiTokens.Space5);

            _statusLabel = new Label
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                VerticalAlignment = VerticalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
            };
            _statusLabel.AddThemeFontSizeOverride("font_size", UiTokens.FontBodySmall);
            bar.AddChild(_statusLabel);

            _hintLabel = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            _hintLabel.AddThemeFontSizeOverride("font_size", UiTokens.FontSmall);
            _hintLabel.AddThemeColorOverride("font_color", UiTokens.Faint);
            bar.AddChild(_hintLabel);

            SetStatus(_pendingStatus, _pendingStatusKind);
            SetHint(_pendingHint);

            var panel = new PanelContainer();
            panel.AddChild(bar);
            var style = new StyleBoxFlat
            {
                BgColor = UiTokens.Surface2,
                BorderColor = UiTokens.Line,
                BorderWidthTop = 1,
                CornerRadiusBottomLeft = UiTokens.RadiusLg,
                CornerRadiusBottomRight = UiTokens.RadiusLg,
                ContentMarginLeft = UiTokens.Space5,
                ContentMarginRight = UiTokens.Space5,
            };
            panel.AddThemeStyleboxOverride("panel", style);
            return panel;
        }

        private static Button CreateTitleButton(string text, Action pressed, Color hoverBg, Color hoverText)
        {
            var btn = new Button
            {
                Text = text,
                CustomMinimumSize = new Vector2(TITLE_BUTTON_SIZE, TITLE_BUTTON_SIZE),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                Flat = true,
                FocusMode = Control.FocusModeEnum.None
            };

            btn.AddThemeColorOverride("font_color", UiTokens.Muted);
            btn.AddThemeColorOverride("font_hover_color", hoverText);
            btn.AddThemeColorOverride("font_pressed_color", hoverText);
            btn.AddThemeColorOverride("font_hover_pressed_color", hoverText);
            btn.AddThemeFontSizeOverride("font_size", UiTokens.FontLead);

            var normalStyle = new StyleBoxFlat { BgColor = Colors.Transparent };
            btn.AddThemeStyleboxOverride("normal", normalStyle);

            var hoverStyle = new StyleBoxFlat
            {
                BgColor = hoverBg,
                CornerRadiusTopLeft = UiTokens.RadiusSm,
                CornerRadiusTopRight = UiTokens.RadiusSm,
                CornerRadiusBottomLeft = UiTokens.RadiusSm,
                CornerRadiusBottomRight = UiTokens.RadiusSm
            };
            btn.AddThemeStyleboxOverride("hover", hoverStyle);
            btn.AddThemeStyleboxOverride("pressed", hoverStyle);
            btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

            btn.Pressed += pressed;
            return btn;
        }

        // ── button actions ──────────────────────────────────────────

        private void OnMaximizeToggle()
        {
            if (_isMaximized)
                RestoreWindow();
            else
                MaximizeWindow();
        }

        private void MaximizeWindow()
        {
            _restorePosition = Position;
            _restoreSize = Size;
            _isMaximized = true;
            _maximizeBtn.Text = "❐";

            Position = Vector2I.Zero;
            Size = (Vector2I)GetGameViewportSize();
        }

        private void RestoreWindow()
        {
            _isMaximized = false;
            _maximizeBtn.Text = "□";

            Position = _restorePosition;
            Size = _restoreSize;
        }

        // ── title bar drag ──────────────────────────────────────────

        private void OnTitleBarInput(InputEvent evt)
        {
            if (evt is InputEventMouseButton mb)
            {
                if (mb.ButtonIndex == MouseButton.Left)
                {
                    if (mb.Pressed)
                    {
                        if (mb.DoubleClick)
                        {
                            OnMaximizeToggle();
                            return;
                        }

                        if (_isMaximized)
                        {
                            _isMaximized = false;
                            _maximizeBtn.Text = "□";
                            var pos = DisplayServer.MouseGetPosition();
                            var viewportSize = GetGameViewportSize();
                            var ratio = pos.X / viewportSize.X;
                            _titleDragWindowStart = new Vector2I(
                                Mathf.RoundToInt(pos.X - _restoreSize.X * ratio),
                                Mathf.RoundToInt(pos.Y - 4));
                            _titleDragMouseStart = new Vector2(pos.X, pos.Y);
                            Size = _restoreSize;
                        }
                        else
                        {
                            _titleDragWindowStart = Position;
                            var mp = DisplayServer.MouseGetPosition();
                            _titleDragMouseStart = new Vector2(mp.X, mp.Y);
                        }
                        _draggingTitle = true;
                    }
                    else
                    {
                        _draggingTitle = false;
                    }
                }
            }
            else if (evt is InputEventMouseMotion && _draggingTitle)
            {
                var mp = DisplayServer.MouseGetPosition();
                var delta = new Vector2(mp.X, mp.Y) - _titleDragMouseStart;
                Position = new Vector2I(
                    _titleDragWindowStart.X + Mathf.RoundToInt(delta.X),
                    _titleDragWindowStart.Y + Mathf.RoundToInt(delta.Y));
            }
        }

        // ── resize zones ────────────────────────────────────────────

        private void AddResizeZones()
        {
            AddEdgeH("ResizeLeft",  ResizeEdge.Left,     0f,  0f,  0f, EDGE_ZONE);
            AddEdgeH("ResizeRight", ResizeEdge.Right,    1f,  1f, -EDGE_ZONE, 0f);
            AddEdgeV("ResizeTop",   ResizeEdge.Top,      0f,  0f,  0f, EDGE_ZONE);
            AddEdgeV("ResizeBottom",ResizeEdge.Bottom,   1f,  1f, -EDGE_ZONE, 0f);

            AddCornerZone("ResizeTL", ResizeEdge.TopLeft,
                0f, 0f, CORNER_ZONE, CORNER_ZONE, Control.CursorShape.Fdiagsize);
            AddCornerZone("ResizeTR", ResizeEdge.TopRight,
                1f, 0f, -CORNER_ZONE, CORNER_ZONE, Control.CursorShape.Bdiagsize);
            AddCornerZone("ResizeBL", ResizeEdge.BottomLeft,
                0f, 1f, CORNER_ZONE, -CORNER_ZONE, Control.CursorShape.Bdiagsize);
            AddCornerZone("ResizeBR", ResizeEdge.BottomRight,
                1f, 1f, -CORNER_ZONE, -CORNER_ZONE, Control.CursorShape.Fdiagsize);
        }

        private void AddEdgeH(string name, ResizeEdge edge,
            float anchorLeft, float anchorRight, float offsetLeft, float offsetRight)
        {
            var zone = MakeResizeZone(name, edge, Control.CursorShape.Hsize);
            zone.AnchorLeft = anchorLeft;
            zone.AnchorRight = anchorRight;
            zone.OffsetLeft = offsetLeft;
            zone.OffsetRight = offsetRight;
            zone.AnchorTop = 0f;
            zone.AnchorBottom = 1f;
            zone.OffsetTop = CORNER_ZONE;
            zone.OffsetBottom = -CORNER_ZONE;
            AddChild(zone);
        }

        private void AddEdgeV(string name, ResizeEdge edge,
            float anchorTop, float anchorBottom, float offsetTop, float offsetBottom)
        {
            var zone = MakeResizeZone(name, edge, Control.CursorShape.Vsize);
            zone.AnchorLeft = 0f;
            zone.AnchorRight = 1f;
            zone.OffsetLeft = CORNER_ZONE;
            zone.OffsetRight = -CORNER_ZONE;
            zone.AnchorTop = anchorTop;
            zone.AnchorBottom = anchorBottom;
            zone.OffsetTop = offsetTop;
            zone.OffsetBottom = offsetBottom;
            AddChild(zone);
        }

        private void AddCornerZone(string name, ResizeEdge edge,
            float anchorH, float anchorV, int offsetW, int offsetH,
            Control.CursorShape cursor)
        {
            var zone = MakeResizeZone(name, edge, cursor);

            zone.AnchorLeft = anchorH;
            zone.AnchorRight = anchorH;
            zone.AnchorTop = anchorV;
            zone.AnchorBottom = anchorV;

            if (offsetW >= 0)
            {
                zone.OffsetLeft = 0;
                zone.OffsetRight = offsetW;
            }
            else
            {
                zone.OffsetLeft = offsetW;
                zone.OffsetRight = 0;
            }

            if (offsetH >= 0)
            {
                zone.OffsetTop = 0;
                zone.OffsetBottom = offsetH;
            }
            else
            {
                zone.OffsetTop = offsetH;
                zone.OffsetBottom = 0;
            }

            AddChild(zone);
        }

        private Control MakeResizeZone(string name, ResizeEdge edge, Control.CursorShape cursor)
        {
            var zone = new Control
            {
                Name = name,
                MouseFilter = Control.MouseFilterEnum.Stop,
                MouseDefaultCursorShape = cursor
            };

            zone.GuiInput += (InputEvent evt) => HandleResizeInput(evt, edge);
            return zone;
        }

        private void HandleResizeInput(InputEvent evt, ResizeEdge edge)
        {
            if (evt is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    _resizing = true;
                    _resizeEdge = edge;
                    var mp = DisplayServer.MouseGetPosition();
                    _resizeMouseStart = new Vector2(mp.X, mp.Y);
                    _resizePosStart = Position;
                    _resizeSizeStart = Size;
                }
                else
                {
                    _resizing = false;
                    _resizeEdge = ResizeEdge.None;
                }
            }
            else if (evt is InputEventMouseMotion && _resizing)
            {
                var mp = DisplayServer.MouseGetPosition();
                ApplyResize(new Vector2(mp.X, mp.Y));
            }
        }

        private void ApplyResize(Vector2 globalMouse)
        {
            var delta = globalMouse - _resizeMouseStart;
            var dx = Mathf.RoundToInt(delta.X);
            var dy = Mathf.RoundToInt(delta.Y);

            int newX = _resizePosStart.X;
            int newY = _resizePosStart.Y;
            int newW = _resizeSizeStart.X;
            int newH = _resizeSizeStart.Y;

            var left = _resizeEdge is ResizeEdge.Left or ResizeEdge.TopLeft or ResizeEdge.BottomLeft;
            var right = _resizeEdge is ResizeEdge.Right or ResizeEdge.TopRight or ResizeEdge.BottomRight;
            var top = _resizeEdge is ResizeEdge.Top or ResizeEdge.TopLeft or ResizeEdge.TopRight;
            var bottom = _resizeEdge is ResizeEdge.Bottom or ResizeEdge.BottomLeft or ResizeEdge.BottomRight;

            if (left) { newX += dx; newW -= dx; }
            if (right) newW += dx;
            if (top) { newY += dy; newH -= dy; }
            if (bottom) newH += dy;

            newX = Mathf.Clamp(newX, 0, MaxSize.X - 100);
            newY = Mathf.Clamp(newY, 0, MaxSize.Y - 100);
            newW = Mathf.Clamp(newW, MinSize.X, MaxSize.X);
            newH = Mathf.Clamp(newH, MinSize.Y, MaxSize.Y);

            if (newX + newW > MaxSize.X)
                newX = MaxSize.X - newW;
            if (newY + newH > MaxSize.Y)
                newY = MaxSize.Y - newH;

            Position = new Vector2I(newX, newY);
            Size = new Vector2I(newW, newH);
        }
    }
}

namespace Cthangover.Mods.Interface.Tool
{
    using Cthangover.Mods.Interface.View;
    using Godot;

    /// <summary>Keeps the editor's last frame visible while its assembly is replaced. Only stable UI/native objects remain in the tree.</summary>
    internal static class ToolDraftRefreshCover
    {
        internal static void Show(Window source, string id)
        {
            var cover = new Window { Name = "ToolDraftCover_" + id, Title = "Обновление редактора", Borderless = true, Unresizable = true, Size = source.Size, Position = source.Position };
            try
            {
                using var frame = ViewportCapture.Take(source);
                if (frame != null)
                {
                    using var texture = ImageTexture.CreateFromImage(frame);
                    var image = new TextureRect { Texture = texture, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale };
                    cover.AddChild(image); image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                }
                var tint = new ColorRect { Color = new Color(0, 0, 0, .65f) }; cover.AddChild(tint); tint.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                var label = new Label { Text = "Обновление редактора…\nЧерновик сохранён", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                label.AddThemeFontSizeOverride("font_size", 26); cover.AddChild(label); label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                source.GetTree().Root.AddChild(cover); cover.Show();
            }
            catch { cover.Free(); throw; }
        }

        internal static void Hide(Node root, string id) { root.GetNodeOrNull<Window>("ToolDraftCover_" + id)?.QueueFree(); }
    }
}

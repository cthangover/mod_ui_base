using Godot;

namespace Cthangover.Mods.Interface.Theming
{
    /// <summary>
    /// Keeps the localized plaque in the artwork's coordinate system so the same
    /// prefab fits both small gallery covers and large detail views.
    /// </summary>
    public partial class DevelopmentFrame : TextureRect
    {
        private Control _plaque;
        private TextureRect _blank;
        private AtlasTexture _blankTexture;

        public override void _Ready()
        {
            _plaque = GetNode<Control>("Plaque");
            _blank = _plaque.GetNode<TextureRect>("Blank");
            // Only the generated blank plaque is used; the original frame keeps
            // its border and transparent opening. Geometry comes from the prefab.
            _blankTexture = new AtlasTexture { Atlas = _blank.Texture, Region = _blank.GetRect() };
            _blank.Texture = _blankTexture;
            Resized += FitPlaque;
            FitPlaque();
        }

        public override void _ExitTree()
        {
            Resized -= FitPlaque;
            if (_blankTexture != null)
            {
                _blank.Texture = null;
                _blankTexture.Dispose();
                _blankTexture = null;
            }
        }

        private void FitPlaque()
        {
            _plaque.Scale = Size / Texture.GetSize();
        }
    }
}

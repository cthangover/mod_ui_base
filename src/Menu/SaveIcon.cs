using Cthangover.Core.Factories.Impls;
using Godot;
using Cthangover.Core.UI.Menu;

namespace Cthangover.Mods.Interface.Menu
{
    /// <summary>
    /// Auto-loading save icon: on _Ready, resolves the "save" texture through
    /// UIIconFactory if no Texture is already assigned. Allows both editor-set
    /// textures and mod-overridden icons without scene modification.
    /// </summary>
    public partial class SaveIcon : TextureRect
    {
        public override void _Ready()
        {
            if (Texture == null)
                Texture = UIIconFactory.Instance.Get("save");
        }
    }
}
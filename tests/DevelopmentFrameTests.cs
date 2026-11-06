namespace Cthangover.Mods.Interface.Theming.Tests
{
    using Cthangover.Core.Mods.Testing;
    using Godot;

    /// <summary>Checks the real shared prefab and decoded alpha, including the release version without an overlay.</summary>
    public class DevelopmentFrameTests
    {
        [ModTest("covers/development_frame")]
        static void DevelopmentFrame(ModTestContext ctx)
        {
            var frame = UiKit.DevelopmentFrame("0.1.0");
            var release = UiKit.DevelopmentFrame("1.0.0");
            try
            {
                ModAssert.True(frame.Visible, "zero major is marked");
                ModAssert.False(release.Visible, "released package is unmarked");
                ModAssert.Equal((int)Control.MouseFilterEnum.Ignore, (int)frame.MouseFilter, "frame does not intercept cover clicks");
                var texture = ((TextureRect)frame).Texture;
                ModAssert.True(texture != null, "generated texture resolves through the mod provider");
                using var image = texture.GetImage();
                ModAssert.True(image.GetPixel(image.GetWidth() / 2, image.GetHeight() / 2).A == 0, "centre leaves the game art visible");
                ModAssert.True(image.GetPixel(image.GetWidth() / 2, 4).A > 0.9f, "hazard tape is visible");
            }
            finally
            {
                frame.Free();
                release.Free();
            }
        }
    }
}

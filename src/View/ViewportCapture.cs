namespace Cthangover.Mods.Interface.View
{
    using System;
    using System.Runtime.InteropServices;
    using Godot;

    /// <summary>
    /// Takes a screenshot of a viewport the way the player sees it.
    /// </summary>
    /// <remarks>
    /// The project renders 2D in HDR (<c>rendering/viewport/hdr_2d</c>): the viewport texture then holds linear
    /// colour, and Godot converts it to sRGB only when it blits to the window. A plain
    /// <c>GetTexture().GetImage().SavePng()</c> wrote those linear values, so every screenshot (the site's
    /// gallery included) came out much darker than the game, shadows most of all. The conversion runs on
    /// float data before the 8-bit quantisation, so dark gradients do not band.
    /// </remarks>
    public static class ViewportCapture
    {
        /// <summary>The current frame of <paramref name="viewport"/> as RGBA8 sRGB, or null without a frame.</summary>
        public static Image Take(Viewport viewport)
        {
            var image = viewport.GetTexture()?.GetImage();
            if (image == null || !viewport.UseHdr2D)
                return image;

            image.Convert(Image.Format.Rgbaf);
            var floats = MemoryMarshal.Cast<byte, float>(image.GetData());
            var bytes = new byte[floats.Length];
            for (var i = 0; i < floats.Length; i++)
            {
                var v = Math.Clamp(floats[i], 0f, 1f);
                if (i % 4 != 3)
                    v = v <= 0.0031308f ? v * 12.92f : 1.055f * MathF.Pow(v, 1f / 2.4f) - 0.055f;
                bytes[i] = (byte)MathF.Round(v * 255f);
            }
            return Image.CreateFromData(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8, bytes);
        }

        /// <summary>Saves the current frame of <paramref name="viewport"/> as a PNG; false without a frame.</summary>
        public static bool SavePng(Viewport viewport, string path)
        {
            var image = Take(viewport);
            return image != null && image.SavePng(path) == Error.Ok;
        }
    }
}

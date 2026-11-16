using System;
using System.Runtime.InteropServices;
using Godot;

namespace Cthangover.Mods.Interface.View
{
    /// <summary>
    /// Takes a screenshot of a viewport the way the player sees it.
    /// </summary>
    /// <remarks>
    /// The project renders 2D in HDR (<c>rendering/viewport/hdr_2d</c>): the viewport texture then holds linear
    /// colour on rendering-device methods, and Godot converts it to sRGB when it blits to the window.
    /// Compatibility already exposes encoded colour, so applying this conversion twice is incorrect. A plain
    /// <c>GetTexture().GetImage().SavePng()</c> wrote those linear values, so every screenshot (the site's
    /// gallery included) came out much darker than the game, shadows most of all. The conversion runs on
    /// float data before the 8-bit quantisation, so dark gradients do not band.
    /// </remarks>
    public static class ViewportCapture
    {
        /// <summary>Returns an owned sRGB frame, or null without a frame; the caller must dispose a returned image.</summary>
        public static Image Take(Viewport viewport)
        {
            return Take(viewport, false);
        }

        /// <summary>
        /// Returns an owned frame; diagnostic documentation may retain monitor layers. The caller disposes the
        /// image.
        /// </summary>
        public static Image Take(Viewport viewport, bool includeDiagnostics)
        {
            using var suppression = includeDiagnostics ? null : ViewportCaptureExclusions.Hide(viewport);
            // The synchronous API must read a freshly rendered clean frame, including callers arriving after FramePostDraw.
            // With no diagnostic layer registered this is exactly the previous path, with no extra draw.
            if (suppression != null)
            {
                RenderingServer.ForceDraw(false, 0);
            }

            var image = viewport.GetTexture()?.GetImage();
            if (image == null || !NeedsLinearConversion(viewport.UseHdr2D, RenderingServer.GetCurrentRenderingMethod()))
            {
                return image;
            }

            using var source = image;
            image.Convert(Image.Format.Rgbaf);
            var floats = MemoryMarshal.Cast<byte, float>(image.GetData());
            var bytes = new byte[floats.Length];
            for (var i = 0; i < floats.Length; i++)
            {
                var v = Math.Clamp(floats[i], 0f, 1f);
                if (i % 4 != 3)
                {
                    v = v <= 0.0031308f ? v * 12.92f : 1.055f * MathF.Pow(v, 1f / 2.4f) - 0.055f;
                }

                bytes[i] = (byte)MathF.Round(v * 255f);
            }

            return Image.CreateFromData(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8, bytes);
        }

        /// <summary>
        /// RD HDR textures need linear-to-sRGB conversion; the compatibility texture is already encoded even with
        /// HDR requested.
        /// </summary>
        public static bool NeedsLinearConversion(bool hdr2D, string actualRenderingMethod)
        {
            return hdr2D && actualRenderingMethod != "gl_compatibility";
        }

        /// <summary>Saves the current frame of <paramref name = "viewport"/> as a PNG; false without a frame.</summary>
        public static bool SavePng(Viewport viewport, string path)
        {
            return SavePng(viewport, path, false);
        }

        /// <summary>
        /// Only monitor documentation opts into diagnostic layers; game screenshot callers keep the two-argument
        /// overload.
        /// </summary>
        public static bool SavePng(Viewport viewport, string path, bool includeDiagnostics)
        {
            using var image = Take(viewport, includeDiagnostics);
            return image != null && image.SavePng(path) == Error.Ok;
        }
    }
}

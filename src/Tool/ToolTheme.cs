using Cthangover.Core.UI.Theming;
using Godot;

namespace Cthangover.Mods.Interface.Tool
{
    /// <summary>
    /// Dense variant of <see cref="CthangoverTheme"/> for developer tool windows.
    /// The game theme is tuned for player panels (buttons padded 18×11, inputs
    /// 12×9), which makes an editor toolbar of ten controls overflow even a
    /// full-HD window. This theme keeps every colour, radius and type variation
    /// of the shared theme — the style guide still holds — and only tightens
    /// the content margins of the boxes, so tools fit more controls without a
    /// second palette. Built once by copying the shared theme and replacing its
    /// flat boxes with tightened duplicates; the shared theme is never mutated.
    /// </summary>
    public static class ToolTheme
    {
        private const int MaxVerticalMargin = 5;
        private const int MaxHorizontalMargin = 10;

        private static Theme instance;

        /// <summary>Shared dense theme instance, built lazily from the game theme.</summary>
        public static Theme Get() => instance ??= Build();

        private static Theme Build()
        {
            var theme = (Theme)CthangoverTheme.Get().Duplicate();
            theme.DefaultFontSize = UiTokens.FontBody;

            foreach (var type in theme.GetTypeList())
            {
                foreach (var name in theme.GetStyleboxList(type))
                {
                    if (theme.GetStylebox(name, type) is not StyleBoxFlat flat)
                        continue;

                    var dense = (StyleBoxFlat)flat.Duplicate();
                    dense.ContentMarginTop = Mathf.Min(dense.ContentMarginTop, MaxVerticalMargin);
                    dense.ContentMarginBottom = Mathf.Min(dense.ContentMarginBottom, MaxVerticalMargin);
                    dense.ContentMarginLeft = Mathf.Min(dense.ContentMarginLeft, MaxHorizontalMargin);
                    dense.ContentMarginRight = Mathf.Min(dense.ContentMarginRight, MaxHorizontalMargin);
                    theme.SetStylebox(name, type, dense);
                }

                if (theme.HasFontSize("font_size", type) && theme.GetFontSize("font_size", type) > UiTokens.FontBody)
                    theme.SetFontSize("font_size", type, UiTokens.FontBody);
            }

            return theme;
        }
    }
}

namespace Cthangover.Mods.Interface.Tool
{
    /// <summary>
    /// Tone of a message in the <see cref="ToolWindow"/> status bar, mapped to the
    /// style guide's inline messages: neutral info, ok (<c>.ct-message--ok</c>)
    /// and error (<c>.ct-message--err</c>).
    /// </summary>
    public enum ToolStatusKind
    {
        Info,
        Ok,
        Error,
    }
}

namespace Cthangover.Mods.Interface.Shell
{
    using System;
    /// <summary>Independent claims compose instead of one panel accidentally unpausing another owner's simulation.</summary>
    [Flags]
    public enum ShellBlock { None = 0, Input = 1, Pointer = 2, Simulation = 4, All = Input | Pointer | Simulation }
}

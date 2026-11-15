namespace Cthangover.Mods.Interface.Shell
{
    using Cthangover.Core.Mods.Consumers;
    /// <summary>Symmetric contributor lifetime, without static references to unloaded dialog/content assemblies.</summary>
    public sealed class ShellContributorConsumer : ModTypeConsumer<IShellContributor>
    {
        public override string Id => "shell_contributors";
        protected override void Add(IShellContributor instance, string modId) => ShellSession.Contributors.Add(instance);
        protected override void Remove(IShellContributor instance, string modId) => ShellSession.Contributors.Remove(instance);
    }
}

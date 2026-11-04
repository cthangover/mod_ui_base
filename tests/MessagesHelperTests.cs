namespace Cthangover.Mods.Interface.Messages.Tests
{
    using System.Collections.Generic;
    using Cthangover.Core.Mods.Testing;
    using Cthangover.Core.Scenes;
    using Cthangover.Core.UI.Messages;
    using Godot;

    /// <summary>Battle/menu notifications must survive a missing shell without allocating orphan UI trees.</summary>
    public class MessagesHelperTests
    {
        [ModTest("messages/without_background_preserves_each_notification")]
        static void WithoutBackground(ModTestContext ctx)
        {
            var root = SceneContextNode.Instance.GetTree().Root;
            var background = root.FindChild("Background", true, false) as Control;
            var originalName = background?.Name ?? default;
            var grouped = background?.IsInGroup("Background") == true;
            var previous = new List<string>();
            while (NotificationBuffer.TryDequeue(out var pendingText)) previous.Add(pendingText);
            try
            {
                // The shared test scene has a background; model a battle without detaching or freeing borrowed UI.
                if (background != null)
                {
                    background.Name = "MessagesContractBackground";
                    if (grouped) background.RemoveFromGroup("Background");
                }
                ModAssert.True(root.FindChild("Background", true, false) == null, "fixture has no shell background");
                for (var i = 0; i < 40; i++) MessagesHelper.AddMessage("reward/" + i);
                ModAssert.Equal(40, NotificationBuffer.PendingCount, "every late reward is buffered exactly once");
                for (var i = 0; i < 40; i++)
                {
                    ModAssert.True(NotificationBuffer.TryDequeue(out var actual), "notification is retained");
                    ModAssert.Equal("reward/" + i, actual, "text and order survive until the shell returns");
                }
                ModAssert.Equal(0, NotificationBuffer.PendingCount, "no duplicate banners remain");
            }
            finally
            {
                if (background != null)
                {
                    background.Name = originalName;
                    if (grouped) background.AddToGroup("Background");
                }
                NotificationBuffer.Clear();
                foreach (var previousText in previous) NotificationBuffer.Add(previousText);
            }
        }
    }
}

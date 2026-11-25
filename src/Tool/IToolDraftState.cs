namespace Cthangover.Mods.Interface.Tool
{
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>Optional document-aware migration across tool assembly replacement. State and attachments must contain no live editor references.</summary>
    public interface IToolDraftState
    {
        JsonObject CaptureToolDraft(string cacheDirectory);
        Task<JsonObject> RestoreToolDraftAsync(JsonObject state, string cacheDirectory, CancellationToken cancellation);
    }
}

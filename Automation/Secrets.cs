using System.Text.Json;

namespace Automation;

internal class Secrets
{
    public required string GitToken { get; init; } = default!;

    public static async ValueTask<Secrets> LoadAsync()
    {
        string path = Path.Combine(Context.ProjectRoot, ".local/.secrets.json");
        using var file = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Secrets>(file) ?? throw new InvalidOperationException();
    }
}

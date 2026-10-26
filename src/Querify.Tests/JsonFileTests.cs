using Querify.Helpers.Tools;
using Xunit;

namespace Querify.Tests;

public sealed class JsonFileTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("querify-tests-").FullName;

    [Fact]
    public async Task ReadAsync_MissingFile_IsMissing()
    {
        var result = await JsonFile.ReadAsync<List<string>>(Path.Combine(_directory, "absent.json"));

        Assert.Equal(JsonFileStatus.Missing, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task WriteThenRead_RoundTrips()
    {
        var path = Path.Combine(_directory, "sous-dossier", "valeurs.json");

        await JsonFile.WriteAllTextAsync(path, JsonFile.Serialize(new List<string> { "a", "b" }));
        var result = await JsonFile.ReadAsync<List<string>>(path);

        Assert.Equal(JsonFileStatus.Loaded, result.Status);
        Assert.Equal(new[] { "a", "b" }, result.Value!);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public async Task ReadAsync_CorruptedFile_IsBackedUp()
    {
        var path = Path.Combine(_directory, "connections.json");
        await File.WriteAllTextAsync(path, "[ \"a\", ");

        var result = await JsonFile.ReadAsync<List<string>>(path);

        Assert.Equal(JsonFileStatus.Corrupted, result.Status);
        Assert.NotNull(result.Error);
        Assert.True(File.Exists(result.BackupPath));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}

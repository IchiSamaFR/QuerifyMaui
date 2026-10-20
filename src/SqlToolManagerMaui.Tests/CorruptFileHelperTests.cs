using SqlToolManagerMaui.Helpers.Tools;
using Xunit;

namespace SqlToolManagerMaui.Tests;

public sealed class CorruptFileHelperTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("sqltoolmanager-tests-").FullName;

    [Fact]
    public void Backup_ExistingFile_KeepsOriginalAndCopiesContent()
    {
        var path = Path.Combine(_directory, "connections.json");
        File.WriteAllText(path, "{ corrompu");

        var backupPath = CorruptFileHelper.Backup(path);

        Assert.NotNull(backupPath);
        Assert.StartsWith(path + ".corrupt-", backupPath, StringComparison.Ordinal);
        Assert.Equal("{ corrompu", File.ReadAllText(backupPath));
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Backup_MissingFile_ReturnsNull()
        => Assert.Null(CorruptFileHelper.Backup(Path.Combine(_directory, "absent.json")));

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}

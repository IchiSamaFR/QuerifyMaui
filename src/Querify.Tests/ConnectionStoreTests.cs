using Newtonsoft.Json;
using Querify.Databases.Connections;
using Querify.Databases.Models.Connections;
using Xunit;

namespace Querify.Tests;

public sealed class ConnectionStoreTests
{
    private const string ConnectionString = "Server=srv;Database=db;User Id=sa;Password=secret";

    private readonly FakeSecrets _secrets = new();
    private readonly FakeStorage _storage = new();
    private readonly ConnectionStore _store;

    public ConnectionStoreTests()
    {
        _store = new ConnectionStore(_secrets, _storage);
    }

    [Fact]
    public async Task SaveAsync_New_WritesDescriptorAndSecret()
    {
        var info = NewInfo("Prod");

        await _store.SaveAsync(info, ConnectionString);

        var written = Assert.Single(_storage.Written!);
        Assert.Equal(info.Id, written.Id);
        Assert.NotSame(info, written);
        Assert.Equal(ConnectionString, _secrets.Values[info.Id]);
    }

    [Fact]
    public void Descriptor_NeverSerializesConnectionString()
    {
        var info = NewInfo("Prod");
        info.ConnectionString = ConnectionString;

        var json = JsonConvert.SerializeObject(info);

        Assert.DoesNotContain("secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(DatabaseConnectionInfo.ConnectionString), json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_SecretRefused_WritesNoDescriptor()
    {
        _secrets.FailOnSet = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _store.SaveAsync(NewInfo("Prod"), ConnectionString));

        Assert.Null(_storage.Written);
    }

    [Fact]
    public async Task SaveAsync_UnreadableStorage_RefusesToOverwrite()
    {
        _storage.Stored = null;

        await Assert.ThrowsAsync<IOException>(() => _store.SaveAsync(NewInfo("Prod"), ConnectionString));

        Assert.Null(_storage.Written);
    }

    [Fact]
    public async Task Subscribe_UnreadableStorage_IsEmptyThenRetried()
    {
        _storage.Stored = null;
        IReadOnlyList<DatabaseConnectionInfo>? received = null;

        await _store.Subscribe(items => received = items);
        Assert.Empty(received!);

        _storage.Stored = [NewInfo("Prod")];
        await _store.RefreshAsync();

        Assert.Single(received!);
        Assert.Equal(2, _storage.ReadCount);
    }

    [Fact]
    public async Task Subscribe_SortsByName()
    {
        _storage.Stored = [NewInfo("b"), NewInfo("a")];
        IReadOnlyList<DatabaseConnectionInfo>? received = null;

        await _store.Subscribe(items => received = items);

        Assert.Equal(new[] { "a", "b" }, received!.Select(c => c.Name));
    }

    [Fact]
    public async Task SaveAsync_Existing_UpdatesSharedInstance()
    {
        var existing = NewInfo("Ancien");
        existing.CreatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        existing.ConnectionString = "ancienne";
        _storage.Stored = [existing];
        await _store.RefreshAsync();

        var edited = existing.Clone();
        edited.Name = "Nouveau";
        await _store.SaveAsync(edited, ConnectionString);

        Assert.Equal("Nouveau", existing.Name);
        Assert.Equal(existing.CreatedAt, edited.CreatedAt);
        Assert.Equal(ConnectionString, existing.ConnectionString);
        Assert.Same(existing, Assert.Single(_storage.Written!));
    }

    [Fact]
    public async Task DeleteAsync_RemovesDescriptorAndSecret_EvenIfVaultFails()
    {
        var info = NewInfo("Prod");
        await _store.SaveAsync(info, ConnectionString);
        _secrets.FailOnRemove = true;

        await _store.DeleteAsync(info.Id);

        Assert.Empty(_storage.Written!);
    }

    [Fact]
    public async Task GetConnectionStringAsync_VaultUnreadable_IsNull()
    {
        _secrets.FailOnGet = true;

        Assert.Null(await _store.GetConnectionStringAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task OpenConnectionAsync_ReadsSecret_OrFailsWhenMissing()
    {
        var info = NewInfo("Prod");
        Assert.False(await _store.OpenConnectionAsync(info));

        _secrets.Values[info.Id] = ConnectionString;
        Assert.True(await _store.OpenConnectionAsync(info));
        Assert.Equal(ConnectionString, info.ConnectionString);

        Assert.True(_store.CloseConnection(info));
        Assert.False(info.IsConnected);
    }

    private static DatabaseConnectionInfo NewInfo(string name) => new() { Id = Guid.NewGuid(), Name = name };

    private sealed class FakeSecrets : IConnectionSecretStore
    {
        public Dictionary<Guid, string> Values { get; } = [];

        public bool FailOnGet { get; set; }

        public bool FailOnSet { get; set; }

        public bool FailOnRemove { get; set; }

        public Task<string?> GetAsync(Guid id)
            => FailOnGet ? throw new InvalidOperationException("coffre") : Task.FromResult(Values.GetValueOrDefault(id));

        public Task SetAsync(Guid id, string connectionString)
        {
            if (FailOnSet)
                throw new InvalidOperationException("coffre");

            Values[id] = connectionString;
            return Task.CompletedTask;
        }

        public void Remove(Guid id)
        {
            if (FailOnRemove)
                throw new InvalidOperationException("coffre");

            Values.Remove(id);
        }
    }

    private sealed class FakeStorage : IConnectionDescriptorStorage
    {
        /// <summary>Contenu lu ; null = source illisible.</summary>
        public List<DatabaseConnectionInfo>? Stored { get; set; } = [];

        public List<DatabaseConnectionInfo>? Written { get; private set; }

        public int ReadCount { get; private set; }

        public Task<List<DatabaseConnectionInfo>?> ReadAsync(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(Stored is null ? null : new List<DatabaseConnectionInfo>(Stored));
        }

        public Task WriteAsync(IReadOnlyList<DatabaseConnectionInfo> items, CancellationToken cancellationToken = default)
        {
            Written = [.. items];
            return Task.CompletedTask;
        }
    }
}

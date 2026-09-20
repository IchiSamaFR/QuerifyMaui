using System.Text.Json.Serialization;
using SqlToolManagerMaui.Models;

namespace SqlToolManagerMaui.Services;

/// <summary>
/// Contexte de serialisation source-genere : obligatoire ici, le projet active
/// les analyseurs trim/AOT et le serialiseur par reflexion y est interdit.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<DatabaseConnectionInfo>))]
[JsonSerializable(typeof(DatabaseConnectionInfo))]
internal sealed partial class ConnectionStoreJsonContext : JsonSerializerContext
{
}

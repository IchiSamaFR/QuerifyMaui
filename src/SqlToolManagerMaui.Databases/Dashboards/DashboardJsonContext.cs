using SqlToolManagerMaui.Core.Models.Dashboards;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlToolManagerMaui.Databases.Dashboards;

/// <summary>Serialisation generee a la compilation : les analyseurs trim/AOT refusent la reflexion.</summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(DashboardDefinition))]
internal sealed partial class DashboardJsonContext : JsonSerializerContext;

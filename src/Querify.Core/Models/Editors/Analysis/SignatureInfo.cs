namespace Querify.Core.Models.Editors.Analysis;

/// <summary>Une surcharge : <c>Prefix</c> + parametres separes par ", " + <c>Suffix</c>.</summary>
/// <param name="Prefix">Ex. "IQueryable&lt;Student&gt; Where(".</param>
/// <param name="Parameters">Ex. "Expression&lt;Func&lt;Student, bool&gt;&gt; predicate".</param>
/// <param name="Description">Precision facultative (type qui declare la methode).</param>
public sealed record SignatureInfo(string Prefix, IReadOnlyList<string> Parameters, string Suffix, string? Description = null);

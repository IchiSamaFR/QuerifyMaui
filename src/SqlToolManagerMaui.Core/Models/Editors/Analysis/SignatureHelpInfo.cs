namespace SqlToolManagerMaui.Core.Models.Editors.Analysis;

/// <summary>Surcharges de la methode dont le curseur est dans la liste d'arguments.</summary>
/// <param name="ActiveSignature">Surcharge retenue par le compilateur, sinon la premiere qui accepte assez d'arguments.</param>
/// <param name="ActiveParameter">Index de l'argument sous le curseur (nombre de virgules qui le precedent).</param>
/// <param name="ArgumentListStart">Position de la parenthese ouvrante : identifie l'appel d'une frappe a l'autre.</param>
public sealed record SignatureHelpInfo(IReadOnlyList<SignatureInfo> Signatures, int ActiveSignature, int ActiveParameter, int ArgumentListStart);

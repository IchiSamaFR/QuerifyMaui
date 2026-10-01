using SqlToolManagerMaui.Models.Databases;

namespace SqlToolManagerMaui.Models.Queries;

/// <summary>Requete a coller dans l'onglet actif de l'editeur, sur la connexion indiquee.</summary>
public sealed record InsertQueryMessage(DatabaseConnectionInfo Connection, string Query);

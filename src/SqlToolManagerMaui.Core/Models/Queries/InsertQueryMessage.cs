using SqlToolManagerMaui.Core.Models.Databases.Connections;

namespace SqlToolManagerMaui.Core.Models.Queries;

/// <summary>Requete a coller dans l'onglet actif de l'editeur, sur la connexion indiquee.</summary>
public sealed record InsertQueryMessage(DatabaseConnectionInfo Connection, string Query);

# Querify

Application Windows pour écrire et exécuter des requêtes **SQL** et **LINQ / Entity Framework** sur SQL Server, avec un habillage Windows 11.

- Éditeur multi-onglets avec coloration syntaxique, autocomplétion, survol et soulignement des erreurs
- Gestion des connexions (chaînes chiffrées) et explorateur de bases
- Exécution SQL façon SSMS (lots `GO`, `PRINT`, plusieurs jeux de résultats, annulation)
- Requêtes LINQ sur un DbContext généré depuis le catalogue, façon LINQPad (`.Dump()`)
- Dashboards `.schx` : cartes tableau / graphique / valeur reliées par des paramètres et une navigation entre pages

## Installation

1. Télécharger `Querify-X.Y.Z-win-x64.zip` depuis les [Releases](https://github.com/IchiSamaFR/SqlToolManagerMaui/releases).
2. Extraire l'archive et lancer `SqlToolManagerMaui.exe`.

Aucun runtime à installer : .NET et le Windows App SDK sont embarqués. Windows 10 1809 ou plus récent, x64.

Les données (connexions, préférences, journaux) sont dans `%LOCALAPPDATA%\IchiSamaFR\fr.ichisamafr.querify`.

## Sécurité

- Les chaînes de connexion complètes ne sont jamais écrites en clair : seul le coffre chiffré de Windows les conserve.
- Un dashboard `.schx` exécute ses requêtes (et, pour les cartes LINQ, du code C#) sur votre poste : l'application demande votre accord avant d'exécuter un fichier qu'elle n'a pas créé. N'autorisez que des fichiers de confiance.

## Compiler depuis les sources

Prérequis : Windows, SDK .NET 10 et workload MAUI.

```powershell
dotnet workload restore src/SqlToolManagerMaui.slnx
dotnet build src/SqlToolManagerMaui.slnx
dotnet test src/SqlToolManagerMaui.Tests/SqlToolManagerMaui.Tests.csproj
```

Architecture, conventions et processus : [.github/instructions.md](.github/instructions.md).

## Publier une version

1. Mettre à jour `Version` dans `src/Directory.Build.props` et [CHANGELOG.md](CHANGELOG.md).
2. Pousser sur `master` un commit titré `Release X.Y.Z` (ou `Release X.Y.Z-rc.N` pour une préversion).

La CI lance les tests, publie le zip self-contained, crée le tag `vX.Y.Z` et la release GitHub. Recette : [docs/recette-0.1.0.md](docs/recette-0.1.0.md).

## Licence

[MIT](LICENSE)

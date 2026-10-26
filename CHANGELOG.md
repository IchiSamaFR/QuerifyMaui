# Changelog

Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/), versions [SemVer](https://semver.org/lang/fr/).

## [0.1.0] - 2026-10-26

Première version publique.

### Ajouté

- Connexions SQL Server : création, test, modification, suppression ; chaîne de connexion chiffrée (SecureStorage), mot de passe masqué à l'édition.
- Explorateur de bases : bases, tables, vues, colonnes.
- Éditeur multi-onglets (CodeEdit.Maui) : coloration syntaxique, autocomplétion, survol, soulignement des erreurs, fichiers récents, réouverture des onglets au démarrage.
- Exécution SQL façon SSMS : lots `GO`, messages `PRINT`, lignes affectées, plusieurs jeux de résultats, annulation.
- Requêtes LINQ / Entity Framework sur un DbContext généré depuis le catalogue (Roslyn), `.Dump()` et sortie console.
- Dashboards `.schx` : concepteur en grille, cartes tableau / graphique / valeur / bouton, paramètres et navigation entre pages, requêtes SQL ou LINQ.
- Accord demandé avant d'exécuter les requêtes d'un `.schx` qui ne vient pas de l'application.
- Journal des erreurs dans le dossier de données (14 jours).

### Limites connues

Voir [docs/recette-0.1.0.md](docs/recette-0.1.0.md#limites-connues-notes-de-version).

[0.1.0]: https://github.com/IchiSamaFR/SqlToolManagerMaui/releases/tag/v0.1.0

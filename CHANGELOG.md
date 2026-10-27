# Changelog

Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/), versions [SemVer](https://semver.org/lang/fr/).

## [Non publié]

### Modifié

- Licence : passage de MIT à PolyForm Noncommercial 1.0.0 (usage personnel et non commercial). La version 0.1.0 reste sous MIT.

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
- Menu Fichier ▸ Nouveau par langage (SQL, LINQ, tableau de bord) ; modèle de prise en main dans les nouveaux scripts, désactivable.
- Raccourcis d'édition façon Visual Studio : indentation à l'Entrée, paires ( [ { " ', retour arrière intelligent, copier / couper / coller de ligne, Ctrl+D, Alt+↑/↓, Ctrl+Maj+K, Ctrl+K Ctrl+C / Ctrl+U, Ctrl+Entrée, Début, crochet associé.
- Zoom de l'éditeur (Ctrl+molette, Ctrl++ / Ctrl+-, Ctrl+0), conservé dans les préférences.
- Rechercher / Remplacer (Ctrl+F / Ctrl+H) en haut à droite de l'éditeur : recherche à la frappe, casse, F3 / Maj+F3, compteur, Tout remplacer.
- Journal des erreurs dans le dossier de données (14 jours).

### Limites connues

Voir [docs/recette-0.1.0.md](docs/recette-0.1.0.md#limites-connues-notes-de-version).

[0.1.0]: https://github.com/IchiSamaFR/Querify/releases/tag/v0.1.0

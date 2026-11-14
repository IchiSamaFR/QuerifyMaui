-- Bienvenue dans Querify. Ce modèle se remplace d'un Ctrl+A ; il se désactive dans
-- Fichier ▸ « Modèle dans les nouveaux fichiers ».
--
-- • Choisissez une connexion dans la barre d'outils de l'onglet.
-- • F5 exécute la sélection, ou tout le script s'il n'y a pas de sélection.
-- • Chaque SELECT a son propre onglet de résultats ; GO sépare les lots, comme dans SSMS.
-- • Ctrl+Espace ouvre l'autocomplétion ; le survol d'un nom affiche son type.
-- • La liste à gauche du bouton Exécuter bascule l'onglet en LINQ (Entity Framework).

DECLARE @Limite int = 10;

SELECT @@VERSION AS Version;

SELECT TOP (@Limite) name AS [Table], create_date AS Creation
FROM sys.tables
ORDER BY create_date DESC;

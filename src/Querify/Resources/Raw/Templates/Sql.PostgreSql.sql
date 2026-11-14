-- Bienvenue dans Querify. Ce modèle se remplace d'un Ctrl+A ; il se désactive dans
-- Fichier ▸ « Modèle dans les nouveaux fichiers ».
--
-- • Choisissez une connexion dans la barre d'outils de l'onglet.
-- • F5 exécute la sélection, ou tout le script s'il n'y a pas de sélection.
-- • Chaque SELECT a son propre onglet de résultats ; les instructions se terminent par « ; ».
-- • Ctrl+Espace ouvre l'autocomplétion ; le survol d'un nom affiche son type.
-- • La liste à gauche du bouton Exécuter bascule l'onglet en LINQ (Entity Framework).

SELECT version() AS "Version";

SELECT table_schema AS "Schema", table_name AS "Table"
FROM information_schema.tables
WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
ORDER BY table_schema, table_name
LIMIT 10;

-- Bienvenue dans Querify. Ce modèle se remplace d'un Ctrl+A ; il se désactive dans
-- Fichier ▸ « Modèle dans les nouveaux fichiers ».
--
-- • Choisissez une connexion dans la barre d'outils de l'onglet.
-- • F5 exécute la sélection, ou tout le script s'il n'y a pas de sélection.
-- • Chaque SELECT a son propre onglet de résultats ; les instructions se terminent par « ; ».
-- • Ctrl+Espace ouvre l'autocomplétion ; le survol d'un nom affiche son type.
-- • La liste à gauche du bouton Exécuter bascule l'onglet en LINQ (Entity Framework).

SELECT VERSION() AS Version;

SELECT table_name AS `Table`, create_time AS Creation
FROM information_schema.tables
WHERE table_schema = DATABASE()
ORDER BY create_time DESC
LIMIT 10;

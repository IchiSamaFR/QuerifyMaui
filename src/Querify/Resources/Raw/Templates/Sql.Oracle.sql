-- Bienvenue dans Querify. Ce modèle se remplace d'un Ctrl+A ; il se désactive dans
-- Fichier ▸ « Modèle dans les nouveaux fichiers ».
--
-- • Choisissez une connexion dans la barre d'outils de l'onglet.
-- • F5 exécute la sélection, ou tout le script s'il n'y a pas de sélection.
-- • Chaque SELECT a son propre onglet de résultats ; une instruction se termine par « ; »,
--   un bloc PL/SQL par une ligne « / », comme sous SQL*Plus.
-- • Les paramètres s'écrivent :nom.
-- • Ctrl+Espace ouvre l'autocomplétion ; le survol d'un nom affiche son type.
-- • La liste à gauche du bouton Exécuter bascule l'onglet en LINQ (Entity Framework).

SELECT banner AS "Version" FROM v$version;

SELECT table_name AS "Table", last_analyzed AS "Analyse"
FROM user_tables
ORDER BY table_name
FETCH FIRST 10 ROWS ONLY;

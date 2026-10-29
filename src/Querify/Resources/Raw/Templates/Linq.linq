// Bienvenue dans Querify : LINQ (C#) sur un DbContext Entity Framework généré depuis la base.
// Ce modèle se remplace d'un Ctrl+A ; il se désactive dans Fichier ▸ « Modèle dans les nouveaux fichiers ».
//
// • Choisissez une connexion : Db est le contexte, il expose les tables de la base.
// • Écrivez du C# tel quel (instructions, usings, classes après les instructions) : F5 l'exécute.
// • Seuls les .Dump() affichent des données : .Dump("Titre") ouvre un onglet de résultats par appel.
// • Paramètres d'un dashboard : utilisez-les par leur nom, sans @ (ex. UserId pour @UserId).
// • Log.Information(...) écrit dans les messages ; Ctrl+Espace ouvre l'autocomplétion, le survol donne le type.
//
// Exemple (remplacez Clients par l'une de vos tables) :

var actifs = Db.Clients.Where(c => c.Actif);
actifs.Count().Dump("Clients actifs");
actifs.OrderBy(c => c.Nom).Take(10).Dump("Dix premiers");

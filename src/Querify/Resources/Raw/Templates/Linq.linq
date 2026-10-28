// Bienvenue dans Querify : LINQ (C#) sur un DbContext Entity Framework généré depuis la base.
// Ce modèle se remplace d'un Ctrl+A ; il se désactive dans Fichier ▸ « Modèle dans les nouveaux fichiers ».
//
// • Choisissez une connexion : le contexte (QueryDbContext, alias du type nommé d'après la base) expose ses tables.
// • La requête est un fichier C# complet, usings compris : F5 appelle la méthode Run de la classe UserQuery.
// • object Run(QueryDbContext context) affiche ce qu'elle renvoie ; void Run(...) n'affiche que ses .Dump().
// • Paramètres d'un dashboard : ajoutez-les à Run sous leur nom, ex. Run(QueryDbContext context, int userId).
// • .Dump("Titre") affiche une valeur dans son propre onglet ; Log.Information(...) écrit dans les messages.
// • Ctrl+Espace ouvre l'autocomplétion ; le survol d'un nom affiche son type.
//
// Exemple (remplacez Clients par l'une de vos tables) :
//
// class UserQuery
// {
//     object Run(QueryDbContext context)
//     {
//         var actifs = context.Clients.Where(c => c.Actif);
//         actifs.Count().Dump("Clients actifs");
//         return actifs.OrderBy(c => c.Nom).Take(10);
//     }
// }

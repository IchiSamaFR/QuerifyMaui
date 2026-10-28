# Recette 0.1.0

A derouler sur le zip publie par la CI (`Querify-0.1.0-rc.N-win-x64.zip`), sur un poste **sans** SDK .NET ni Windows App SDK installes.

## Installation et donnees

- [ ] Le zip s'extrait et `Querify.exe` demarre sans runtime a installer
- [ ] Titre de la fenetre, icone et version (0.1.0) corrects
- [ ] Poste ayant deja utilise l'app : les connexions de `%LOCALAPPDATA%\User Name\fr.ichisamafr.querify` sont reprises dans `%LOCALAPPDATA%\IchiSamaFR\fr.ichisamafr.querify`
- [ ] Apres reprise, les chaines de connexion s'ouvrent (coffre SecureStorage recopie lui aussi)
- [ ] Journal ecrit dans `...\IchiSamaFR\fr.ichisamafr.querify\Data\logs`

## Connexions

- [ ] Creer, tester, modifier, supprimer une connexion SQL Server
- [ ] Le mot de passe est masque a l'edition et conserve si non ressaisi
- [ ] `connections.json` ne contient aucune chaine de connexion ni mot de passe
- [ ] Explorateur : bases, tables, colonnes se deplient

## Editeur SQL

- [ ] Nouveau / Ouvrir / Enregistrer / Enregistrer sous / Renommer / fichiers recents
- [ ] Nouveau ▸ SQL / LINQ / Tableau de bord : modele du langage insere, onglet non marque modifie ; preference « Modele dans les nouveaux fichiers » respectee
- [ ] Entree garde l'indentation ; apres `{` un niveau de plus ; entre `{}` bloc sur 3 lignes ; Maj+Entree idem
- [ ] Paires ( [ { " ' : fermeture auto, selection entouree, fermant saute ; rien dans un commentaire ou une chaine ; `}` sur ligne vide desindente
- [ ] Retour arriere : paire vide, niveau d'indentation dans les espaces de tete
- [ ] Ctrl+C / Ctrl+X sans selection : ligne entiere ; Ctrl+V la colle au-dessus ; Ctrl+D ; Alt+Haut / Alt+Bas ; Ctrl+Maj+K
- [ ] Tab / Maj+Tab, Ctrl+K Ctrl+C / Ctrl+K Ctrl+U, Ctrl+Entree / Ctrl+Maj+Entree, Debut (deux appuis)
- [ ] Crochet associe encadre (ignore dans chaines et commentaires)
- [ ] Zoom Ctrl+molette / Ctrl++ / Ctrl+- / Ctrl+0, pourcentage en barre d'etat, conserve au redemarrage
- [ ] Ctrl+F / Ctrl+H : barre en haut a droite, recherche a la frappe, Aa, F3 / Maj+F3, « 2 sur 5 », Tout remplacer annule d'un seul Ctrl+Z
- [ ] Coloration, numeros de ligne, retour a la ligne, indentation
- [ ] Execution du document, puis de la seule selection
- [ ] Script a plusieurs lots `GO`, dont un en erreur : les suivants s'executent, ligne d'erreur recalee
- [ ] `PRINT`, nombre de lignes affectees, plusieurs jeux de resultats
- [ ] Annulation d'une requete longue (`WAITFOR DELAY '00:01'`)
- [ ] Plus de 10 000 lignes : resultat tronque et signale
- [ ] Autocompletion (frappe et Ctrl+Espace), survol, soulignement des erreurs
- [ ] La selection est conservee apres un aller-retour du focus (clic ailleurs puis retour au clavier)

## Editeur LINQ

- [ ] Requete simple, instructions avec `var` / `return`, plusieurs `.Dump()`
- [ ] `Log.Information` / `Log.Warning` / `Log.Error` dans l'onglet Messages (couleur selon le niveau), SQL genere affiche
- [ ] Erreur de compilation soulignee et listee
- [ ] Annulation d'une boucle infinie : la main revient, message « se termine en arriere-plan »
- [ ] Completion, survol et aide a la signature

## Dashboard .schx

- [ ] Creer un dashboard, placer / redimensionner des cartes (double clic), pages en onglets
- [ ] Cartes SQL et LINQ : tableau, graphique, valeur, bouton de navigation
- [ ] Parametres appliques, navigation et fil d'Ariane
- [ ] Fichier recu d'ailleurs : la demande d'accord s'affiche avant toute requete (mention des cartes LINQ)
- [ ] Refus : cartes en message, bouton Appliquer repose la question
- [ ] Accord donne : plus de demande a la reouverture, ni apres renommage

## Espace de travail

- [ ] Fermeture avec un fichier modifie : popup Enregistrer / Ne pas enregistrer / Annuler
- [ ] Reouverture des onglets (onglets sans titre et leurs modifications compris)
- [ ] Erreur inattendue : signalee dans la barre d'etat, l'application reste ouverte

## Limites connues (notes de version)

- Windows 10 1809+ x64 uniquement, SQL Server uniquement.
- L'editeur repose sur CodeEdit.Maui, encore en preversion (0.4.0-preview.2).
- Une requete LINQ annulee n'est pas interrompue : elle se termine en arriere-plan avec sa connexion.
- L'accord d'execution d'un .schx est retenu par chemin : un fichier remplace au meme emplacement n'est pas redemande.
- Theme sombre uniquement.

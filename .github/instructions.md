# SqlToolManagerMaui - Instructions Partagées

**Source centralisée pour Claude et Copilot. À consulter en priorité.**

---

## À propos du projet

SqlToolManagerMaui est une application **desktop Windows** .NET MAUI permettant d'écrire et d'exécuter des requêtes SQL / EntityFramework : éditeur multi-onglets avec coloration syntaxique, gestion des connexions aux bases, le tout dans un habillage **Windows 11**. Architecture MVVM basée sur CommunityToolkit.Mvvm.

---

## Contexte fonctionnel

* **Pages principales** : Accueil (`MainPage`), Requête (`QueryPage`). Les connexions vivent dans l'explorateur d'objets (volet gauche).
* **Requête** : éditeur de texte multi-onglets façon Notepad, posé sur `CodeEdit.Maui`. Nouveau / Ouvrir / Enregistrer / Enregistrer sous, options d'édition (retour à la ligne, numéros de ligne, indentation), barre d'état (chemin du fichier, Ln/Col).
* **Explorateur d'objets** (façon SSMS) : arbre Serveur > Bases (+ dossier « Bases de données système ») > Tables / Vues / Procédures stockées > Colonnes (nom + type façon SSMS : « Id (PK, int, not null) »), chargé à l'ouverture de chaque nœud. Ajout / modification / test d'une connexion dans la popup « Se connecter » (CommunityToolkit `Popup`).
* **Moteur supporté** : SQL Server uniquement (Microsoft.Data.SqlClient). L'architecture prévoit l'ajout d'autres moteurs.
* **Exécution des requêtes** : F5 / bouton Exécuter sur la connexion choisie. Exécute la sélection, ou tout le document à défaut. Découpage sur `GO` comme SSMS, résultats par onglet d'éditeur (un onglet par jeu de résultats + Messages), annulation possible.

**Règles à respecter absolument** :
* Ne jamais générer du code Xamarin.Forms — toujours les équivalents .NET MAUI.
* Ne jamais mettre la chaîne de connexion complète dans un modèle affichable ou dans le JSON en clair.
* Ne pas casser la logique existante des pages déjà livrées.

---

## Architecture

### Couches

**Projet principal (`SqlToolManagerMaui`)** :
* **Views/Pages** : `ContentPage` XAML, UI uniquement, héritent de `AbstractPage`.
* **Views/Controls** : contrôles XAML réutilisables, groupés par domaine (`Editors/`).
* **ViewModels** : logique de présentation, héritent de `AbstractVM` (qui étend `ObservableObject`). Les VM de page sont dans `ViewModels/Pages/`.
* **Services** : logique métier et accès aux ressources (disque, SecureStorage, bases), groupés par domaine (`Databases/`, `Editors/`).
* **Models** : modèles locaux, groupés par domaine (`Databases/`, `Editors/`).
* **Ioc** : conteneur statique et enregistrement centralisé des dépendances.
* **Resources/Styles** : tokens (couleurs, tailles, épaisseurs) et styles par contrôle.

**Bibliothèque UI (`SqlToolManagerMaui.UI`)** :
* **Inputs** : contrôles de saisie maison (`ButtonView`, `TextField`, `PickerField`, `IntField`, `PinInput`...), chacun scindé en `X.cs` + `X.bindable.cs`.
* **Converters** : convertisseurs XAML réutilisables.
* **Behaviors / Attachments / Extensions** : comportements et helpers transverses.

**Important** : respecter strictement cette stratification. Un contrôle générique (non lié au métier SQL) va dans `SqlToolManagerMaui.UI`, un contrôle spécifique à l'application va dans `Views/Controls/`.

### Structure du dépôt

Convention **"src layout"** : le code source (solution + projets) est sous `src/`, les fichiers de config du dépôt (`.github/`, `CLAUDE.md`, `.gitignore`) restent à la racine.

```
SqlToolManagerMaui/
├── src/
│   ├── SqlToolManagerMaui/                     # Projet MAUI principal (Windows)
│   │   ├── Ioc/
│   │   │   ├── Ioc.cs                          # Service locator statique
│   │   │   └── ServiceCollectionExtensions.cs  # Scan des classes [AutoRegister]
│   │   ├── Models/
│   │   │   ├── Databases/                      # DatabaseConnectionInfo, Draft, Provider
│   │   │   ├── Editors/                        # EditorDocument, EditorConfiguration, EditorSelection
│   │   │   └── Queries/                        # QueryExecutionResult, QueryResultSet, QueryMessage
│   │   ├── Services/
│   │   │   ├── Databases/                      # ConnectionStore, testeurs par moteur
│   │   │   ├── Editors/                        # EditorFileService (FilePicker / FileSaver)
│   │   │   └── Queries/                        # Exécuteurs par moteur, découpage GO
│   │   ├── ViewModels/
│   │   │   ├── AbstractVM.cs                   # Base : navigation, chargement
│   │   │   ├── AbstractVM_Busy.cs              # Base : gestion des tâches / IsBusy
│   │   │   ├── ShellVM.cs                      # État du rail de navigation
│   │   │   └── Pages/                          # QueryVM, Connections/ (ConnectionExplorerVM, ConnectionEditorVM)
│   │   ├── Views/
│   │   │   ├── Pages/                          # AbstractPage + pages de l'app
│   │   │   └── Controls/                       # NavigationRail + Editors/ (menus, onglets)
│   │   ├── Resources/
│   │   │   ├── Styles/Tokens/                  # Colors, FontSizes, Thickness
│   │   │   ├── Styles/Controls/                # Un fichier de styles par contrôle
│   │   │   └── Raw/SyntaxHighlights/Sql.xshd   # Coloration syntaxique SQL
│   │   ├── Platforms/                          # Code spécifique plateforme (Windows)
│   │   ├── Properties/AppXmlns.tt              # Génère les XmlnsDefinition
│   │   ├── AppShell.xaml                       # Shell + rail de navigation
│   │   └── MauiProgram.cs                      # Point d'entrée et configuration DI
│   ├── SqlToolManagerMaui.UI/                  # Bibliothèque UI réutilisable
│   │   ├── Inputs/                             # Contrôles de saisie (+ .bindable.cs)
│   │   ├── Converters/                         # Convertisseurs XAML
│   │   ├── Behaviors/ Attachments/ Extensions/
│   │   └── Properties/AppXmlns.tt
│   ├── .editorconfig
│   └── SqlToolManagerMaui.slnx                 # Solution (format slnx)
├── .github/
│   ├── instructions.md                         # CE FICHIER — source centralisée
│   └── copilot-instructions.md                 # Référence vers ce fichier
└── CLAUDE.md                                   # Point d'entrée instructions Claude
```

---

## Pattern MVVM

* Tous les ViewModels de page héritent de `AbstractVM` → `ObservableObject` (CommunityToolkit.Mvvm).
* Utiliser `[ObservableProperty]` sur des **propriétés partielles** (`public partial bool IsX { get; set; }`), pas sur des champs.
* Utiliser `[RelayCommand]` pour les commandes, `[NotifyPropertyChangedFor]` / `[NotifyCanExecuteChangedFor]` pour les dépendances.
* Les pages héritent de `AbstractPage`, qui fixe le `BindingContext`, relaie la navigation (`NavigatedTo` / `NavigatedFrom`) et lie `IsEnabled` à `IsNotBusy`.
* Chaque page expose `public static readonly string PAGE_ROUTE` et surcharge `PageRoute`.
* Les tâches longues passent par `RunAsBusyAsync` / `StartAsBusy` (`AbstractVM_Busy`) : compteurs de tâches, `LifetimeToken`, exceptions absorbées.
* Le chargement initial se fait dans `LoadDatasBusyAsync`, appelé uniquement si `IsLoadingDatasRequested`.

---

## Injection de dépendances

* Services, pages et ViewModels se déclarent eux-mêmes avec `[AutoRegister]` (`Ioc/AutoRegisterAttribute.cs`) : singleton et type concret par défaut, `Lifetime = ServiceLifetime.Transient` et/ou `As = typeof(IMonService)` au besoin. `Ioc/ServiceCollectionExtensions.cs` scanne l'assembly ; plusieurs classes `As` la même interface sont toutes enregistrées.
* Injection **par constructeur** exclusivement.
* `Ioc.GetRequiredService<T>()` est un dernier recours, réservé au code qui ne peut pas recevoir ses dépendances (code-behind, factories).
* Pages et ViewModels sont enregistrés en **singleton** (application desktop mono-fenêtre).

---

## Bibliothèques clés

* **CommunityToolkit.Mvvm** — Infrastructure MVVM (ObservableObject, RelayCommand).
* **CommunityToolkit.Maui / .Markup** — Comportements, `FileSaver`, snackbar.
* **CodeEdit.Maui** — Éditeur de code avec coloration syntaxique (`.UseCodeEditor()`). Exige `Microsoft.Maui.Controls` >= 10.0.101.
* **AathifMahir.Maui.MauiIcons.Fluent** — Icônes Fluent (`.UseFluentMauiIcons()`).
* **Microsoft.Data.SqlClient** — Accès SQL Server.
* **Newtonsoft.Json** — Sérialisation du fichier de connexions.

---

## Standards de codage

### Principes généraux

* **Framework cible** : .NET MAUI `net10.0-windows10.0.19041.0` (win-x64).
* **Langage** : C# (`LangVersion preview`, nullable activé) pour la logique, XAML pour l'UI.
* **Lisibilité** : code clair et auto-explicatif, préférer le simple au sophistiqué.
* **Gestion d'erreurs** : exceptions structurées, jamais avalées silencieusement hors des helpers prévus (`RunAsBusyAsync`).
* **SOLID** et injection par constructeur.
* **Async/Await** obligatoire pour tout I/O (disque, SecureStorage, base).
* **Magic numbers** : remplacer par des constantes ou des tokens XAML.
* **Une classe par fichier**, namespaces **file-scoped** (imposé par `.editorconfig`).

### Conventions de nommage

* **Classes & Interfaces** : PascalCase (`QueryVM`, `IConnectionStore`).
* **Méthodes & Propriétés** : PascalCase, suffixe `Async` pour les méthodes asynchrones.
* **Champs privés** : `_camelCase`.
* **Paramètres** : camelCase.
* **Pages** : suffixe `Page` (`QueryPage`). **ViewModels** : suffixe `VM` (`QueryVM`).
* **Contrôles** : nom du rôle sans préfixe (`NavigationRail`, `FileTabItem`, `ViewMenuBarItem`).

### Style de code

* **Accolades** : style Allman.
* **Indentation** : 4 espaces en C#, alignement des attributs en XAML (un attribut par ligne).
* **Usings** : en dehors du namespace, inutilisés supprimés.

### BindableProperty

Les `BindableProperty` sont générées par l'attribut `[BindableProperty]` de CommunityToolkit.Maui (`using CommunityToolkit.Maui;`, classe `partial`, avertissement `MCTEXP001` neutralisé dans les csproj) : on déclare uniquement la propriété `partial`, le champ `XxxProperty` est généré.

```csharp
/// <summary>Bascule l'affichage des numeros de ligne.</summary>
[BindableProperty]
public partial ICommand? ToggleLineNumbersCommand { get; set; }

/// <summary>Langage de l'onglet actif.</summary>
[BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnLanguageChanged))]
public partial SyntaxLanguage Language { get; set; } = SyntaxLanguage.Sql;

private static void OnLanguageChanged(BindableObject bindable, object oldValue, object newValue)
    => ((ViewMenuBarItem)bindable).Refresh();
```

* Valeur par défaut = initialiseur de la propriété (absent = `default`).
* Options via l'attribut : `DefaultBindingMode`, `PropertyChangedMethodName`, `PropertyChangingMethodName`, `CoerceValueMethodName`, `ValidateValueMethodName`, `DefaultValueCreatorMethodName` — toujours avec `nameof(...)` vers une méthode statique à la signature MAUI (pas de lambda).
* `{ get; private set; }` produit une propriété en lecture seule (`BindablePropertyKey` généré).
* Propriété `public`, `internal` ou `protected internal` uniquement ; masquage d'une propriété héritée avec `new partial`.
* La `<summary>` va sur la propriété ; types référence nullables (`ICommand?`).
* Un `propertyChanged` n'est ajouté qu'en **dernier recours** : préférer un binding XAML.
* Pour un contrôle riche, sortir les propriétés dans un fichier partiel `X.bindable.cs` (convention de `SqlToolManagerMaui.UI.Inputs` et de `AbstractPage`).

### Conventions XAML

* **Namespaces implicites** : `MauiAllowImplicitXmlnsDeclaration` est actif, les préfixes `c:` (controls), `m:` (models), `v:` (views), `vm:` (viewmodels), `inp:` (inputs), `conv:`, `icons:` n'ont pas à être déclarés. Ils viennent des `XmlnsDefinition` de `Properties/AppXmlns.cs`, généré par `AppXmlns.tt`.
* **Le T4 ne tourne pas au build** : après l'ajout d'un dossier de namespace, mettre `AppXmlns.cs` à jour (relancer l'outil personnalisé ou éditer à la main).
* **Bindings compilés** : `MauiStrictXamlCompilation` est actif. Toujours renseigner `x:DataType` (sur la page, ou en ligne dans le binding : `x:DataType=c:FileTabItem`).
* **Un contrôle ne connaît pas les ViewModels** : il expose des `BindableProperty` et se lie à lui-même via `{Binding Source={RelativeSource AncestorType={x:Type c:MonControle}}, Path=MaProp, x:DataType=c:MonControle}`. C'est la page hôte qui branche le VM.
* **Boutons** : utiliser `Button` (texte + icône via `ImageSource="{icons:Fluent Icon=Xxx, IconColor=..., IconSize=20}"`) avec les styles `PrimaryButton`, `SecondaryButton`, `DangerButton` (ou `TextButton` pour un bouton icône sans fond). `inp:ButtonView` reste réservé aux boutons à contenu personnalisé. Pas d'`ImageButton` ni de `Border` + TapGesture.
* **Icônes** : `{icons:Fluent Icon=Xxx}` sur les contrôles connus de MauiIcons, `{x:Static fluent:FluentIcons.Xxx}` sur une propriété custom.
* **Styles et couleurs** : passer par les tokens (`Resources/Styles/Tokens/`) et les styles existants, ne pas coder de couleur en dur.

### Langue

* **Commentaires en code** : français, succincts — expliquer le *pourquoi*, jamais le *quoi*.
* **Noms de classes/méthodes** : anglais (convention .NET).
* **Textes d'interface** : français.
* **Messages de commit et documentation** : français.

---

## Directives d'implémentation

* Préférer les solutions simples et pragmatiques, éviter la sur-ingénierie.
* Produire du code prêt pour la production : pas de TODO ni d'implémentation partielle.
* **Le moins de code-behind possible** : privilégier le déclaratif (bindings, triggers, styles). Un handler d'événement se justifie quand la bibliothèque tierce n'expose pas de binding (cas du `CodeEditor`, qui notifie par événements).
* Le code spécifique plateforme va dans `Platforms/` avec les gardes `#if WINDOWS`.
* Les analyseurs trim/AOT sont actifs : éviter la réflexion non annotée.

---

## Prise de décision

Lors de la création d'une feature :

1. **Identifier l'intention** — quelle est la feature exacte ?
2. **Modèles** — créer ou réutiliser dans `Models/[Domaine]/`.
3. **Service** — si logique métier ou accès à une ressource, créer dans `Services/[Domaine]/` derrière une interface.
4. **ViewModel** — créer dans `ViewModels/Pages/`, hériter de `AbstractVM`.
5. **Page XAML** — créer dans `Views/Pages/`, hériter de `AbstractPage`, exposer `PAGE_ROUTE`.
6. **Contrôles** — extraire un contrôle dès qu'un bloc XAML pollue la page ou devient réutilisable (`Views/Controls/[Domaine]/`) ; générique et sans métier → `SqlToolManagerMaui.UI`.
7. **Navigation** — déclarer un `ShellContent` dans `AppShell.xaml` PUIS un `c:NavigationRailItem` dans le rail.
8. **Enregistrement DI** — marquer Service, Page et ViewModel avec `[AutoRegister]`.
9. **Namespaces XAML** — si un nouveau dossier est créé, mettre `AppXmlns.cs` à jour.
10. **Compilation & validation** — `dotnet build` (voir Validation).

---

## Points clés spécifiques à ce projet

### Navigation (menu latéral Windows 11)

* Pas de TabBar, pas de header : `AppShell.xaml` utilise `FlyoutBehavior="Locked"` et remplace le flyout par `Views/Controls/NavigationRail`.
* `AppShell.xaml.cs` masque sous Windows le hamburger natif du `NavigationView` WinUI, son item Settings et le bouton retour.
* `ShellVM` porte `IsPaneOpen` / `PaneWidth` (260 étendu, 48 compact), `CurrentRoute`, `TogglePaneCommand`, `NavigateCommand` (`GoToAsync("//route")`).

### Éditeur (page Requête)

* Un onglet = un `EditorDocument` (CodeDocument + chemin + IsDirty). Le namespace est `Models.Editors` (et non `Editor`) pour ne pas masquer `Microsoft.Maui.Controls.Editor`.
* Le `CodeEditor` notifie par **événements** (`ContentChanged`, `SelectionChanged`, `DocumentChanged`), pas par bindings : le code-behind de `QueryPage` les relaie au VM.
* Ouverture par `FilePicker`, « Enregistrer sous » par `FileSaver` (CommunityToolkit), UTF-8 sans BOM.
* **Barre de menus** : les actions sont des contrôles `MenuBarItem` dédiés (`Views/Controls/Editors/*MenuBarItem`), branchés depuis `QueryPage` via leurs `BindableProperty`. Sous Windows, MAUI rend le MenuBar dans le `MauiToolbar` du Shell : la page doit porter `Shell.NavBarIsVisible="True"` **et** un `Title` non vide (cf. dotnet/maui#6303).
* `MenuFlyoutItem` n'a pas d'état « coché » : les bascules changent leur libellé via un `DataTrigger` sur une propriété d'état du contrôle.

### Exécution des requêtes

* `QueryVM.ExecuteQueryCommand` (async, `IncludeCancelCommand`) : texte = `Selection.SelectedText` s'il n'est pas vide, sinon `ActiveDocument.EditorText`. Volontairement **hors** `RunAsBusyAsync`, qui désactiverait la page et donc le bouton Annuler.
* **Ajouter un moteur** = une classe `[AutoRegister(As = typeof(IQueryExecutor))]` ; `QueryExecutorResolver` le découvre seul.
* `SqlServerQueryExecutor` : une connexion par exécution (pas de session conservée entre deux F5), `CommandTimeout = 0`, lots `GO` / `GO n` découpés par `SqlBatchSplitter` (ignore les GO dans chaînes et commentaires bloc), un lot en erreur n'arrête pas les suivants, lignes d'erreur recalées sur le script exécuté. Les erreurs SQL sont rendues dans les messages, pas levées.
* Plafond de `MaxRowsPerResultSet` (10 000) lignes par jeu, cellules converties en texte côté service (NULL = `null`).
* Le résultat est rangé sur l'`EditorDocument` d'origine (`LastResult`, `IsExecuting`) : chaque onglet garde ses résultats.
* `Views/Controls/Queries/QueryResultGrid` est construit en code (colonnes connues à l'exécution) ; les lignes (`QueryResultRowView`) sont remplies au recyclage, sans binding. Styles dans `Resources/Styles/Controls/QueryResultStyles.xaml`.

### Connexions

* Stockage mixte : métadonnées dans `connections.json` (AppDataDirectory, écriture atomique via fichier `.tmp`), chaîne de connexion complète chiffrée dans le `SecureStorage` sous la clé `connectionstring:{id}`. `DatabaseConnectionInfo` ne contient **jamais** la chaîne complète.
* Toutes les écritures passent par un sémaphore, le fichier est réécrit en entier.
* **Ajouter un moteur** = une classe `[AutoRegister(As = typeof(IConnectionTester))]` ; `ConnectionTesterResolver` le découvre seul.
* La popup « Se connecter » (`ConnectionEditorPopup` + `ConnectionEditorVM`, transient) travaille sur un `DatabaseConnectionDraft` (clone) pour qu'« Annuler » ne modifie rien. Elle est ouverte par `IConnectionDialogService.EditAsync` et fermée par le VM via `IPopupService.ClosePopupAsync(shell, Guid?)` (Id enregistré, ou null).
* Après un ajout / une modification / une suppression, `ConnectionsChangedMessage` est publié (WeakReferenceMessenger) : `QueryVM` recharge son Picker.
* **Explorateur** : `ConnectionExplorerVM` garde l'arbre (`Roots`), affiché par le contrôle générique `v:TreeView` de `SqlToolManagerMaui.UI/Views` (MAUI n'en fournit pas) : nœuds imbriqués, `ChildrenPath` / `IsExpandedPath` / `CanExpandPath`, `ExpandingCommand` appelé à chaque ouverture pour le chargement paresseux. Aucun `ExplorerNode` ne porte la chaîne : elle est relue dans le coffre à chaque chargement. Une erreur de chargement devient un nœud « Erreur : ... ».
* **Catalogue d'un moteur** = une classe `[AutoRegister(As = typeof(IDatabaseSchemaReader))]` (résolu par `DatabaseSchemaReaderResolver`), sur le modèle des testeurs.

### Pièges connus

* `SqlToolManagerMaui.UI` doit être chargé **avant** le premier parsing XAML (forcé dans `MauiProgram`), sinon les `XmlnsDefinition` de ses converters sont introuvables.
* En MAUI 10, `Page.DisplayAlert` est obsolète : utiliser `DisplayAlertAsync`.
* Les `ResourceDictionary` sans `x:Class` sont analysés à l'exécution : une erreur de clé ne se voit pas à la compilation.

---

## Anti-patterns à éviter

* Ne PAS mettre de logique métier ou de navigation dans le code-behind des pages.
* Ne PAS référencer un ViewModel depuis un contrôle (`x:DataType="vm:..."` dans `Views/Controls/`) : passer par des `BindableProperty`.
* Ne PAS empiler des dizaines de lignes de XAML dans une page quand un contrôle dédié fait le travail.
* Ne PAS utiliser un `propertyChanged` là où un binding XAML suffit.
* Ne PAS accéder au conteneur via `Ioc` quand l'injection par constructeur est possible.
* Ne PAS écrire la chaîne de connexion complète en clair (JSON, logs, modèle affichable).
* Ne PAS ignorer async/await pour les accès disque, SecureStorage ou base.
* Ne PAS utiliser les API Xamarin.Forms — toujours les équivalents .NET MAUI.
* Ne PAS contourner `AbstractVM` / `AbstractPage` pour une nouvelle page.

---

## Validation

### Toujours compiler après les changements

Le projet ne cible que Windows :

```bash
dotnet build src/SqlToolManagerMaui.slnx
```

`MauiStrictXamlCompilation` transforme les avertissements de binding en erreurs : un `x:DataType` manquant casse le build.

---

**Dernière mise à jour** : 2026-09-26

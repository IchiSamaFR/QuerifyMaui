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
* **Dashboards (`.schx`)** : onglet à trois vues (Dashboard / Concepteur / Source). Une grille de 12 colonnes × 6 lignes (allongée par les cartes) porte des cartes (tableau, valeur, graphiques barres / lignes / secteurs, texte, bouton) alimentées par des requêtes SQL qui utilisent les paramètres d'entrée du dashboard (`@UserId`…). Un bouton, ou un clic sur une ligne de tableau, ouvre une autre page en transmettant des valeurs (fil d'Ariane, retour). Fichier > Nouveau dashboard.
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
* **Views/Controls** : contrôles XAML réutilisables, groupés par domaine : `Navigation/` (rail), `Editors/` (`CodeEditor`, `Menus/` pour la barre de menus, `Completion/`), `Queries/` (résultats), `Dashboards/`. Un sous-dossier = un sous-namespace, à ajouter dans `Properties/AppXmlns.cs`.
* **ViewModels** : logique de présentation, héritent de `AbstractVM` (qui étend `ObservableObject`). Les VM de page sont dans `ViewModels/Pages/`.
* **Services** : logique propre à l'application (disque, SecureStorage, popups, coloration, complétion), groupés par domaine (`Databases/`, `Editors/`). L'accès aux moteurs de base vit dans `Databases`, les contrats communs aux langages dans `Languages.Commons`, leurs implémentations dans `Languages.[Nom]`, les résolveurs dans `Languages`.
* **Models / Enums** : dans `SqlToolManagerMaui.Core` (plus de dossier `Models` dans le projet principal).
* **Ioc** : conteneur statique et enregistrement centralisé des dépendances.
* **Resources/Styles** : tokens (couleurs, tailles, épaisseurs) et styles par contrôle.

**Bibliothèque UI (`SqlToolManagerMaui.UI`)** :
* **Inputs** : contrôles de saisie maison (`ButtonView`, `TextField`, `PickerField`, `IntField`, `PinInput`...), chacun scindé en `X.cs` + `X.bindable.cs`.
* **Converters** : convertisseurs XAML réutilisables.
* **Behaviors / Attachments / Extensions** : comportements et helpers transverses.

**Noyau (`SqlToolManagerMaui.Core`)** : ne dépend pas du projet principal.
* **Models / Enums** : modèles et énumérations, groupés par domaine (`Dashboards/`, `Databases/`, `Editors/`, `Queries/`).
* **Services** : contrats applicatifs uniquement (`ICompletionService`).
* **Ioc** : `AutoRegisterAttribute`.

**Bases de données (`SqlToolManagerMaui.Databases`)** : tout ce qui parle à un moteur, indépendamment du langage. Référence `Core` uniquement.
* **`Connections/`** : `IConnectionTester` (un par `DatabaseProvider`) et `ConnectionTesterResolver`.
* **`Dashboards/`** : `DashboardSerializer` (JSON `.schx`, source-gen, énumérations en texte), `IDashboardQueryReader` (un par `DatabaseProvider`) + résolveur, `IDashboardDataService`.
* **`Schemas/`** : `IDatabaseSchemaReader` (un par `DatabaseProvider`), `DatabaseSchemaReaderResolver`, `IDatabaseSchemaCache` / `DatabaseSchemaCache` (catalogues en mémoire pour la complétion et LINQ).
* **`SqlServer/`** : `SqlServerConnectionTester`, `SqlServerSchemaReader`, `SqlServerDashboardQueryReader`.

**Langages** : `Core` ← `Databases` ← `Languages.Commons` ← `Languages.Sql` / `Languages.Linq` ← `Languages` ← projet principal (qui référence aussi `Databases`).
* **`Languages.Commons`** : contrats implémentés par les langages — `IQueryExecutor` (un par `SyntaxLanguage`), `ICompletionContextAnalyzer`, `ICompletionProvider` — plus `QueryExecutionLimits`.
* **`Languages.[Nom]`** : un projet par langage, qui ne référence que `Languages.Commons`. Les langages ne se référencent pas entre eux.
* **`Languages`** : seul projet de langage référencé par l'application. Référence tous les langages, expose `IQueryExecutorResolver` (par `SyntaxLanguage`) et `LanguageAssemblies.All` (assemblies à scanner).
* **`Languages.Sql`** : `Queries/` (`SqlQueryExecutor`, point d'entrée du langage, qui délègue au `ISqlProviderExecutor` du moteur), `SqlServer/` (exécuteur, découpage `GO`) et `Completion/` (`SqlLexer`, analyseur de contexte).
* **`Languages.Linq`** : générateur de modèle EF, compilateur Roslyn, exécuteur, cache de modèle, complétion C#. Les dépendances EF Core et Roslyn restent confinées ici.

**Autres projets** : `SqlToolManagerMaui.Helpers` (extensions et outils partagés), `SqlToolManagerMaui.Tests` (xUnit, référence `Core` et les projets de langage).

**Important** : respecter strictement cette stratification. Un contrôle générique (non lié au métier SQL) va dans `SqlToolManagerMaui.UI`, un contrôle spécifique à l'application va dans `Views/Controls/`.

### Structure du dépôt

Convention **"src layout"** : le code source (solution + projets) est sous `src/`, les fichiers de config du dépôt (`.github/`, `CLAUDE.md`, `.gitignore`) restent à la racine.

```
SqlToolManagerMaui/
├── src/
│   ├── SqlToolManagerMaui/                     # Projet MAUI principal (Windows)
│   │   ├── Ioc/
│   │   │   ├── Ioc.cs                          # Service locator statique
│   │   │   └── ServiceCollectionExtensions.cs  # Scan des [AutoRegister] (app + projets de langage)
│   │   ├── Services/
│   │   │   ├── Databases/                      # ConnectionStore, dialogue
│   │   │   ├── Editors/                        # EditorFileService, coloration, fournisseurs de complétion
│   │   │   └── Queries/                        # QueryExecutorResolver
│   │   ├── ViewModels/
│   │   │   ├── AbstractVM.cs                   # Base : navigation, chargement
│   │   │   ├── AbstractVM_Busy.cs              # Base : gestion des tâches / IsBusy
│   │   │   ├── ShellVM.cs                      # État du rail de navigation
│   │   │   └── Pages/                          # QueryVM, Queries/ (QueryConnectionVM, QueryExecutionVM, DashboardVM, EditorPreferencesVM, StatusBarVM, IQueryWorkspace), Connections/ (ConnectionExplorerVM, ConnectionEditorVM)
│   │   ├── Views/
│   │   │   ├── Pages/                          # AbstractPage + pages de l'app
│   │   │   └── Controls/                       # Navigation/, Editors/ (Menus/, Completion/), Queries/, Dashboards/
│   │   ├── Resources/
│   │   │   ├── Styles/Tokens/                  # Colors, FontSizes, Thickness
│   │   │   ├── Styles/Controls/                # Un fichier de styles par contrôle
│   │   │   └── Raw/SyntaxHighlights/Sql.xshd   # Coloration syntaxique SQL
│   │   ├── Platforms/                          # Code spécifique plateforme (Windows)
│   │   ├── Properties/AppXmlns.tt              # Génère les XmlnsDefinition
│   │   ├── AppShell.xaml                       # Shell + rail de navigation
│   │   └── MauiProgram.cs                      # Point d'entrée et configuration DI
│   ├── SqlToolManagerMaui.Core/                # Noyau : modèles, enums, contrats, AutoRegister
│   │   ├── Models/                             # Databases/, Editors/, Queries/
│   │   ├── Enums/                              # Databases/, Editors/
│   │   ├── Services/                           # Contrats applicatifs : Editors/Completion/
│   │   └── Ioc/                                # AutoRegisterAttribute
│   ├── SqlToolManagerMaui.Databases/           # Moteurs : Connections/, Schemas/, SqlServer/
│   ├── SqlToolManagerMaui.Languages/           # Agrégateur : résolveur d'exécution + LanguageAssemblies
│   ├── SqlToolManagerMaui.Languages.Commons/   # Contrats communs aux langages
│   ├── SqlToolManagerMaui.Languages.Sql/       # Langage SQL
│   │   ├── Queries/                            # SqlQueryExecutor, ISqlProviderExecutor
│   │   ├── SqlServer/                          # Exécuteur, découpage GO
│   │   └── Completion/                         # SqlLexer, SqlCompletionContextAnalyzer
│   ├── SqlToolManagerMaui.Languages.Linq/      # Langage LINQ (EF Core + Roslyn)
│   ├── SqlToolManagerMaui.Helpers/             # Extensions et outils partagés
│   ├── SqlToolManagerMaui.Tests/               # Tests xUnit (Linq, analyseur SQL)
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

* Services, pages et ViewModels se déclarent eux-mêmes avec `[AutoRegister]` (`Core/Ioc/AutoRegisterAttribute.cs`) : singleton et type concret par défaut, `Lifetime = ServiceLifetime.Transient` et/ou `As = typeof(IMonService)` au besoin. `Ioc/ServiceCollectionExtensions.cs` scanne l'assembly de l'application, celle de `Databases` et celles de `LanguageAssemblies.All` (projet `Languages`) ; plusieurs classes `As` la même interface sont toutes enregistrées.
* Injection **par constructeur** exclusivement.
* `Ioc.GetRequiredService<T>()` est un dernier recours, réservé au code qui ne peut pas recevoir ses dépendances (code-behind, factories).
* Pages et ViewModels sont enregistrés en **singleton** (application desktop mono-fenêtre).

---

## Bibliothèques clés

* **CommunityToolkit.Mvvm** — Infrastructure MVVM (ObservableObject, RelayCommand).
* **CommunityToolkit.Maui / .Markup** — Comportements, `FileSaver`, snackbar.
* **CodeEdit.Maui** — Éditeur de code avec coloration syntaxique (`.UseCodeEditor()`). Exige `Microsoft.Maui.Controls` >= 10.0.101.
* **AathifMahir.Maui.MauiIcons.Fluent** — Icônes Fluent (`.UseFluentMauiIcons()`).
* **LiveChartsCore.SkiaSharpView.Maui** (LiveCharts2) — Graphiques des cartes de dashboard (`.UseSkiaSharp().UseLiveCharts()`).
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
* **Contrôles en XAML + code-behind** : tout contrôle visuel de `Views/Controls/` (app et `SqlToolManagerMaui.UI`) est un couple `X.xaml` / `X.xaml.cs`, même dérivé de `MenuFlyoutItem`, `MenuFlyoutSubItem` ou `Grid`. La structure fixe, les bindings internes, les triggers et les gestes vont dans le XAML (`x:Name="Root"`, liaisons via `{Binding MaProp, Source={x:Reference Root}, x:DataType=v:MonControle}`) ; le code-behind ne garde que les `[BindableProperty]`, `InitializeComponent()` et ce que le XAML ne peut pas exprimer (contenu dépendant de colonnes connues à l'exécution, accès au natif WinUI). Une partie dynamique se construit en code **dans** un conteneur nommé du XAML (`Header`, `Rows`...), jamais en réaffectant tout le `Content`. Exceptions : les non-vues (`TabBarItem`, contrôleurs) et `SqlToolManagerMaui.UI.Inputs`, dont les classes de base (`InputField`...) sont dérivées (une classe dérivée d'un contrôle XAML ne peut pas avoir son propre XAML).
* **Apparence portée par un parent** : un sous-élément interne (ex. `TreeViewNode`) expose son propriétaire en propriété (`Owner`), affectée **avant** `InitializeComponent()`, et le XAML s'y lie (`Owner.ItemHoverColor`).
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

* Pas de TabBar, pas de header : `AppShell.xaml` utilise `FlyoutBehavior="Locked"` et remplace le flyout par `Views/Controls/Navigation/NavigationRail`.
* `AppShell.xaml.cs` masque sous Windows le hamburger natif du `NavigationView` WinUI, son item Settings et le bouton retour.
* `ShellVM` porte `IsPaneOpen` / `PaneWidth` (260 étendu, 48 compact), `CurrentRoute`, `TogglePaneCommand`, `NavigateCommand` (`GoToAsync("//route")`).

### Éditeur (page Requête)

* Un onglet = un `EditorDocument` (CodeDocument + chemin + IsDirty). Le namespace est `Models.Editors` (et non `Editor`) pour ne pas masquer `Microsoft.Maui.Controls.Editor`.
* Le `CodeEditor` notifie par **événements** (`ContentChanged`, `SelectionChanged`, `DocumentChanged`), pas par bindings : le code-behind de `QueryPage` les relaie au VM.
* Ouverture par `FilePicker`, « Enregistrer sous » par `FileSaver` (CommunityToolkit), UTF-8 sans BOM.
* **Barre de menus** : les actions sont des contrôles `MenuBarItem` dédiés (`Views/Controls/Editors/Menus/*MenuBarItem`), regroupés dans `MainMenuBarView` (liée à `MainMenuBarVM`) et posés sur la page par `AttachTo`. Chaque entrée publie un enum `Weak*MenuBarType` (ou un message `OpenRecentFileMessage` / `SetLanguageMessage`) via `IWeakReferenceService` ; la page active s'y abonne en `MessageScope.WhenAlive` et renvoie l'état affiché (coches, fichiers récents, exécution) par `MenuBarStateMessage`. Les commandes d'édition, propres au `CodeEditor`, sont relayées par `QueryPage`. Sous Windows, MAUI rend le MenuBar dans le `MauiToolbar` du Shell : la page doit porter `Shell.NavBarIsVisible="True"` **et** un `Title` non vide (cf. dotnet/maui#6303).
* `MenuFlyoutItem` n'a pas d'état « coché » : les bascules changent leur libellé via un `DataTrigger` sur une propriété d'état du contrôle.

### Exécution des requêtes

* `QueryExecutionVM.ExecuteQueryCommand` (async, `IncludeCancelCommand`) : texte = `Selection.SelectedText` s'il n'est pas vide, sinon `ActiveDocument.EditorText`. Volontairement **hors** `RunAsBusyAsync`, qui désactiverait la page et donc le bouton Annuler.
* `QueryExecutionVM` exécute via `IQueryExecutorResolver.Resolve(document.Language)` : il ne connaît aucun langage en particulier.
* `QueryVM` est découpé en sous-VM (`ViewModels/Pages/Queries/`) exposés en propriétés (`Connection`, `Execution`, `Dashboard`, `Preferences`, `Status`) et liés en XAML par `{Binding Dashboard.XxxCommand}`. Ce sont des `ObservableObject` (pas des `AbstractVM`, réservé aux pages), injectés en `Transient` ; ils accèdent à l'état partagé (onglets, connexion, barre d'état, `LifetimeToken`, `RunAsBusyAsync` / `StartAsBusy`) via `IQueryWorkspace`, implémenté par `QueryVM` et passé par `Attach`. `QueryVM` câble les dépendances croisées : un changement d'onglet actif ou de connexion (`QueryConnectionVM.SelectedConnectionChanged`) rafraîchit `Execution.RefreshCanExecute()` et les cartes du dashboard.
* **Ajouter un moteur** = une classe `[AutoRegister(As = typeof(ISqlProviderExecutor))]` dans `Languages.Sql/[Moteur]/` ; `SqlQueryExecutor` le découvre seul.
* `SqlServerQueryExecutor` (`Languages.Sql/SqlServer/`) : une connexion par exécution (pas de session conservée entre deux F5), `CommandTimeout = 0`, lots `GO` / `GO n` découpés par `SqlBatchSplitter` (ignore les GO dans chaînes et commentaires bloc), un lot en erreur n'arrête pas les suivants, lignes d'erreur recalées sur le script exécuté. Les erreurs SQL sont rendues dans les messages, pas levées.
* Plafond de `QueryExecutionLimits.MaxRowsPerResultSet` (`Languages.Commons`, 10 000) lignes par jeu, cellules converties en texte côté service (NULL = `null`).
* Le résultat est rangé sur l'`EditorDocument` d'origine (`LastResult`, `IsExecuting`) : chaque onglet garde ses résultats.
* **LINQ** : le script devient le corps d'une méthode générée (`LinqCompiler.BuildQuerySource`), le contexte porte le nom de la base. Le premier `return` termine le script ; `.Dump("titre")` (extension générée dans le même source, façon LINQPad) ajoute un jeu de résultats sans l'arrêter et rend la valeur. La valeur du `return` n'est affichée que si elle n'a pas déjà été dumpée ; des instructions sans `return` n'affichent que leurs Dump.
* `Views/Controls/Queries/QueryResultGrid` a un squelette XAML ; en-tête et gabarit de ligne sont posés en code (colonnes connues à l'exécution) ; les lignes (`QueryResultRowView`) sont remplies au recyclage, sans binding. Styles dans `Resources/Styles/Controls/QueryResultStyles.xaml`.

### Dashboards (`.schx`)

* Un onglet a un `Kind` (`DocumentKind.Script` ou `Dashboard`, choisi par l'extension `.schx`) et, pour un dashboard, un `DashboardMode` (`View`, `Design`, `Source`). Le **JSON reste la source** : le concepteur écrit dans la définition (`DashboardCardState`, `DashboardParameterValue`, `DashboardPageState` écrivent directement dans les objets `*Definition`), la session lève `DefinitionChanged` et `DashboardVM` resérialise dans `EditorText` (`DashboardSession.MarkWritten` évite une relecture). La vue Source est relue au retour (`LoadedText`). F5 est désactivé sur ces onglets.
* **Format** (`Core/Models/Dashboards/DashboardDefinition`) : `connection` (nom d'une connexion enregistrée, jamais la chaîne), `database`, `columns` (12) / `rows` (6, minimum), `parameters` (`name`, `label`, `type` : Text, Integer, Decimal, Date, DateTime, Boolean, Guid, `default`), `startPage`, `pages` (`id`, `title`, `cards`). Carte : `column` / `row` (base 0), `columnSpan` / `rowSpan`, `view` (Grid, Value, BarChart, LineChart, PieChart, Text, Button), `query`, `text`, `labelColumn`, `valueColumns`, `navigation` (`page` + `parameters` : `{parameter, column}` pour une colonne de la ligne cliquée, `{parameter, value}` pour un texte fixe ou `@AutreParam`).
* **Paramètres** : communs à toutes les pages, envoyés en `SqlParameter` typés (`DashboardParameterDefinition.TryConvert`), jamais concaténés. Chaque étape du fil d'Ariane (`DashboardPageState`) garde ses valeurs et ses résultats : Retour ne relit rien. « Appliquer » (ou Entrée) relit les cartes de la page.
* **Lecture d'une carte** : premier jeu de résultats ayant des colonnes, 1 000 lignes max, cartes lues en parallèle ; une nouvelle lecture annule la précédente ; changer de connexion relit la page affichée.
* **Rendu** : `Views/Controls/Dashboards/` — `DashboardView` (paramètres, fil d'Ariane, grille à deux calques : cases du concepteur dessous, cartes dessus ; lignes / colonnes créées en code), `DashboardCardView`, `DashboardChartView` (LiveCharts2 : graphiques déclarés en XAML, séries posées en code), `DashboardDesignerPanel`. `QueryResultGrid.RowTappedCommand` rend les lignes cliquables.
* **Concepteur** : clic sur une case libre = nouvelle carte 4 × 2 ; clic sur une carte = sélection (pas de navigation) ; glisser une carte sur une case = déplacement ; le panneau édite titre, rendu, requête (« Exécuter la requête »), colonnes, page cible et paramètres transmis (« Param = Colonne », « = @Param », « = 'texte' »), position / taille, pages et paramètres.
* Ajouter un moteur = aussi une classe `[AutoRegister(As = typeof(IDashboardQueryReader))]` dans `Databases/[Moteur]/`.

### Connexions

* Stockage mixte : métadonnées dans `connections.json` (AppDataDirectory, écriture atomique via fichier `.tmp`), chaîne de connexion complète chiffrée dans le `SecureStorage` sous la clé `connectionstring:{id}`. `DatabaseConnectionInfo` ne contient **jamais** la chaîne complète.
* Toutes les écritures passent par un sémaphore, le fichier est réécrit en entier.
* **Ajouter un moteur** = une classe `[AutoRegister(As = typeof(IConnectionTester))]` dans `Databases/[Moteur]/` ; `ConnectionTesterResolver` le découvre seul.
* La popup « Se connecter » (`ConnectionEditorPopup` + `ConnectionEditorVM`, transient) travaille sur un `DatabaseConnectionDraft` (clone) pour qu'« Annuler » ne modifie rien. Elle est ouverte par `IConnectionDialogService.EditAsync` et fermée par le VM via `IPopupService.ClosePopupAsync(shell, Guid?)` (Id enregistré, ou null).
* Après un ajout / une modification / une suppression, `ConnectionsChangedMessage` est publié (WeakReferenceMessenger) : `QueryConnectionVM` recharge son Picker.
* **Explorateur** : `ConnectionExplorerVM` garde l'arbre (`Roots`), affiché par le contrôle générique `v:TreeView` de `SqlToolManagerMaui.UI/Views` (MAUI n'en fournit pas) : nœuds imbriqués, `ChildrenPath` / `IsExpandedPath` / `CanExpandPath`, `ExpandingCommand` appelé à chaque ouverture pour le chargement paresseux. Aucun `ExplorerNode` ne porte la chaîne : elle est relue dans le coffre à chaque chargement. Une erreur de chargement devient un nœud « Erreur : ... ».
* **Catalogue d'un moteur** = une classe `[AutoRegister(As = typeof(IDatabaseSchemaReader))]` dans `Databases/[Moteur]/` (résolu par `DatabaseSchemaReaderResolver`), sur le modèle des testeurs.

### Préférences utilisateur

* `UserPreferences` (`Core/Models/Settings/`) : fichiers récents (10 max, du plus récent au plus ancien, sans doublon) et options d'affichage de l'éditeur (`EditorConfiguration`).
* Persistées par `IUserPreferencesStore` (`Services/Settings/`) dans `preferences.json` (AppDataDirectory, écriture atomique, sémaphore). `GetAsync` / `UpdateAsync` rendent une copie : modifier le résultat n'enregistre rien.
* `EditorPreferencesVM` charge les préférences (appelé par `QueryVM.LoadDatasBusyAsync`), enregistre à chaque changement d'`EditorConfiguration` et ajoute un fichier aux récents à l'ouverture et à l'« Enregistrer sous ». Un récent introuvable est retiré de la liste.
* Menu Fichier > Fichiers récents : `MenuFlyoutSubItem` sans ItemsSource, reconstruit en code par `FileMenuBarItem` à chaque changement de `RecentFiles`.
* Dossier nommé `Settings` (et non `Preferences`) pour ne pas masquer `Microsoft.Maui.Storage.Preferences`.

### Pièges connus

* `SqlToolManagerMaui.UI` doit être chargé **avant** le premier parsing XAML (forcé dans `MauiProgram`), sinon les `XmlnsDefinition` de ses converters sont introuvables.
* En MAUI 10, `Page.DisplayAlert` est obsolète : utiliser `DisplayAlertAsync`.
* Les `ResourceDictionary` sans `x:Class` sont analysés à l'exécution : une erreur de clé ne se voit pas à la compilation.

---

## Anti-patterns à éviter

* Ne PAS mettre de logique métier ou de navigation dans le code-behind des pages.
* Ne PAS référencer un ViewModel depuis un contrôle (`x:DataType="vm:..."` dans `Views/Controls/`) : passer par des `BindableProperty`.
* Ne PAS empiler des dizaines de lignes de XAML dans une page quand un contrôle dédié fait le travail.
* Ne PAS construire en C# l'arborescence visuelle d'un contrôle (`new Grid { ... }`, `SetBinding` internes) quand un `.xaml` peut la porter.
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

**Dernière mise à jour** : 2026-10-10

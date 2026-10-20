# SqlToolManagerMaui - Instructions Partagées

**Source centralisée pour Claude et Copilot. À consulter en priorité.**

---

## À propos du projet

SqlToolManagerMaui (nom affiché : **Querify**) est une application **desktop Windows** .NET MAUI permettant d'écrire et d'exécuter des requêtes SQL / EntityFramework : éditeur multi-onglets avec coloration syntaxique, gestion des connexions aux bases, le tout dans un habillage **Windows 11**. Architecture MVVM basée sur CommunityToolkit.Mvvm.

---

## Contexte fonctionnel

* **Une seule page** (`MainPage`) : explorateur d'objets à gauche, onglets de documents à droite, barre de menus Fichier / Édition / Affichage / Requête. Deux types d'onglets : script (SQL ou LINQ, `CodeEditorView`) et dashboard `.schx` (`DashboardView`).
* **Éditeur** : multi-onglets façon Notepad, posé sur `CodeEdit.Maui`. Nouveau / Ouvrir / Enregistrer / Enregistrer sous / Fermer, fichiers récents, options d'édition (retour à la ligne, numéros de ligne, indentation), barre d'état. Fermer un onglet modifié demande d'enregistrer ; les onglets ouverts sont rouverts au démarrage (option).
* **Explorateur d'objets** (façon SSMS) : arbre Serveur > Bases (+ dossier « Bases de données système ») > Tables / Vues / Procédures stockées > Colonnes (« Id (PK, int, not null) »), chargé à l'ouverture de chaque nœud. Ajout / modification / test d'une connexion dans la popup « Se connecter » (CommunityToolkit `Popup`).
* **Moteur supporté** : SQL Server uniquement (Microsoft.Data.SqlClient). L'architecture prévoit l'ajout d'autres moteurs.
* **Dashboards (`.schx`)** : onglet à trois vues (Dashboard / Concepteur / Source). Une grille de 12 colonnes × 6 lignes (allongée par les cartes) porte des cartes (tableau, valeur, graphiques barres / lignes / secteurs, texte, bouton) alimentées par des requêtes SQL qui utilisent les paramètres d'entrée du dashboard (`@UserId`…). Un bouton, ou un clic sur une ligne de tableau, ouvre une autre page en transmettant des valeurs (fil d'Ariane, retour). Fichier > Nouveau tableau de bord.
* **Connexion par onglet** : le sélecteur de l'en-tête est partagé, mais chaque onglet garde sa connexion (`AbstractSession.ConnectionId`, mémorisée avec les onglets) ; un nouvel onglet prend celle de l'onglet courant.
* **Exécution des requêtes** : F5 / bouton Exécuter sur la connexion de l'onglet. Exécute la sélection, ou tout le document à défaut. Découpage sur `GO` comme SSMS, résultats par onglet d'éditeur (un onglet par jeu de résultats + Messages), annulation possible.

**Règles à respecter absolument** :
* Ne jamais générer du code Xamarin.Forms — toujours les équivalents .NET MAUI.
* Ne jamais mettre la chaîne de connexion complète dans le JSON en clair ni dans les journaux.
* Ne pas casser la logique existante des pages déjà livrées.

---

## Architecture

### Couches et dépendances

`Helpers` ← `Databases` (indépendante) ← `Core` ← `Languages.Commons` ← `Languages.Sql` / `Languages.Linq` ← `Languages` ← application. `UI` ne référence que `Helpers`. L'application référence `Core`, `Databases`, `Languages` et `UI`.

**Application (`SqlToolManagerMaui`)** :
* **Views/Pages** : `MainPage` (hérite de `AbstractPage`), `Connections/` (explorateur, popup de connexion), `Documents/` (`CodeEditorView`, `DashboardView`, `DocumentViewSelector`).
* **Views/Controls** : contrôles XAML propres à l'application, par domaine : `Editors/` (`CodeEditor`, `Completion/`, coloration), `Queries/` (résultats), `Dashboards/`. Un sous-dossier = un sous-namespace, à ajouter dans `Properties/AppXmlns.cs`.
* **ViewModels** : `Pages/` (`MainVM` en trois fichiers partiels : base, `Files`, `Workspace`), `Pages/Documents/` (`DocumentVM<TSession>`, `CodeEditorVM`, `DashboardVM`), `Pages/Shared/` (`DatabaseConnectionVM`, `StatusBarVM`), `Pages/Connections/`, `Controls/Menus/MainMenuBarVM`.
* **Services** (derrière une interface, par domaine) : `Databases/` (`ConnectionStore`, dialogue de connexion), `Dialogs/` (`IDialogService` : questions posées depuis un VM), `Diagnostics/` (`FileLogWriter`), `Editors/` (fichiers, coloration, complétion), `Settings/` (préférences, onglets mémorisés).
* **Ioc** : locator statique + scan des `[AutoRegister]`. **Platforms/Windows** : code WinUI (`WindowCloseGuard`).
* **Resources/Styles** : tokens (couleurs, tailles, épaisseurs) et un fichier de styles par contrôle.

**Bibliothèque UI (`SqlToolManagerMaui.UI`)** — générique, sans métier SQL :
* `ViewModels/` (`AbstractVM`, `AbstractVM_Busy`), `Views/` (`AbstractPage`, `AbstractContentView`, `Controls/` : `Layouts`, `Menus`, `Tabs`, `Trees`, `ViewSelector`).
* `Inputs/` (contrôles de saisie maison : `ButtonView`, `TextField`, `PickerField`, `EditorField`, chacun scindé en `X.cs` + `X.bindable.cs`), `Converters/`, `Behaviors/`, `MarkupExtensions/`.
* `Extensions/` : `WeakReferenceMessengerExtensions` (journalisation), `FireAndForgetExtensions`. `Models/Messagings/WeakLog`.

**Noyau (`SqlToolManagerMaui.Core`)** : modèles et énumérations de l'application par domaine (`Dashboards/`, `Databases/`, `Editors/`, `Queries/`, `Settings/`), `Serialization/` (`DashboardSerializer`, System.Text.Json source-gen), `Ioc/AutoRegisterAttribute`. Aucun service.

**Bases de données (`SqlToolManagerMaui.Databases`)** : librairie **indépendante** (aucune référence de projet). Tout ce qui parle à un moteur, indépendamment du langage et des dashboards. Services enregistrés par `services.AddDatabases()`, pas par `[AutoRegister]`.
* `Models/`, `Enums/` : `DatabaseConnectionInfo`, `ConnectionTestResult`, catalogue, `DatabaseSchemaSnapshot`, `QueryResultSet`, `QueryParameter`, `DatabaseProvider`...
* `Connections/`, `Queries/`, `Schemas/` : contrats par moteur (`IConnectionTester`, `IQueryReader`, `IDatabaseSchemaReader`) et leurs résolveurs, `IQueryDataService`, `IDatabaseSchemaCache`.
* `Common/KeyedServiceMap<TKey, TService>` : base de tous les résolveurs (une implémentation par clé, doublon signalé au démarrage).
* `SqlServer/` : `SqlServerConnectionTester`, `SqlServerSchemaReader`, `SqlServerQueryReader`.

**Langages** :
* `Languages.Commons` : `IQueryExecutor` (un par `SyntaxLanguage`), `ICompletionContextAnalyzer`, `ICompletionProvider`, `QueryExecutionLimits`.
* `Languages.[Nom]` : un projet par langage, ne référence que `Languages.Commons`. `Languages.Sql` : `Queries/` (`SqlQueryExecutor` → `ISqlProviderExecutor` du moteur), `SqlServer/` (exécuteur, `SqlBatchSplitter`), `Completion/`. `Languages.Linq` : générateur de modèle EF, compilateur Roslyn, exécuteur, cache, complétion C# (EF Core et Roslyn restent confinés ici).
* `Languages` : seul projet de langage référencé par l'application ; `IQueryExecutorResolver`, `LanguageAssemblies.All`, `InsertQueryMessage`.

**Autres** : `Helpers` (extensions, `Tools/` : `JsonFile`, `TextFileEncoding`, `CorruptFileHelper`, `ConnectionStringPasswordHelper`), `Tests` (xUnit, référence les projets de langage, donc `Core`, `Databases` et `Helpers`).

### Structure du dépôt

```
SqlToolManagerMaui/
├── .github/
│   ├── instructions.md              # CE FICHIER — source centralisée
│   ├── copilot-instructions.md
│   └── workflows/build.yml          # CI : build Windows + tests
├── CLAUDE.md
└── src/
    ├── .editorconfig
    ├── Directory.Build.props        # Réglages communs à tous les projets
    ├── Directory.Packages.props     # Versions de tous les paquets NuGet
    ├── SqlToolManagerMaui.slnx
    ├── SqlToolManagerMaui/          # Application MAUI (Windows)
    ├── SqlToolManagerMaui.Core/
    ├── SqlToolManagerMaui.Databases/
    ├── SqlToolManagerMaui.Helpers/
    ├── SqlToolManagerMaui.Languages(.Commons|.Sql|.Linq)/
    ├── SqlToolManagerMaui.Tests/
    └── SqlToolManagerMaui.UI/
```

---

## Pattern MVVM

* Les VM de page héritent de `AbstractVM` → `ObservableObject`. Les VM d'onglets héritent de `DocumentVM<TSession>`.
* `[ObservableProperty]` sur des **propriétés partielles** (`public partial bool IsX { get; set; }`), jamais sur des champs.
* `[RelayCommand]` pour les commandes, `[NotifyPropertyChangedFor]` / `[NotifyCanExecuteChangedFor]` pour les dépendances.
* Les pages héritent de `AbstractPage` : `BindingContext`, navigation (`NavigatedTo` / `NavigatedFrom`), `IsEnabled` lié à `IsNotBusy`. Chaque page expose `PAGE_ROUTE`.
* Tâches longues : `RunAsBusyAsync` / `StartAsBusy` (`AbstractVM_Busy`) — compteurs, `LifetimeToken`, page grisée pendant une tâche bloquante, exception journalisée par `LogError`.
* Chargement initial dans `LoadDatasBusyAsync`, appelé uniquement si `IsLoadingDatasRequested`.

### Erreurs et journalisation

* **Un seul point d'entrée** : `WeakReferenceMessenger.Default.LogError(exception, message)` (ou `LogInfo`). Pas d'`ILogger`.
* Abonnés : `StatusBarVM` affiche les erreurs (« Erreur : message ») ; `FileLogWriter` écrit `logs/app-aaaaMMjj.log` dans `AppDataDirectory` (14 jours). Le message est donc rédigé pour l'utilisateur.
* Une tâche lancée sans `await` passe par `.FireAndForget("message")` : jamais de `_ = XAsync()`.
* Exceptions non gérées (AppDomain, thread UI WinUI, tâches non observées) : journalisées au démarrage (`MauiProgram`, `Platforms/Windows/App.xaml.cs`).

---

## Injection de dépendances

* Services, pages et VM se déclarent avec `[AutoRegister]` (singleton et type concret par défaut ; `Lifetime = ServiceLifetime.Transient` et/ou `As = typeof(IMonService)` au besoin). `Ioc/ServiceCollectionExtensions.cs` appelle `AddDatabases()` puis scanne l'application et `LanguageAssemblies.All` ; plusieurs classes `As` la même interface sont toutes enregistrées (les résolveurs les reçoivent en `IEnumerable`).
* Injection **par constructeur** exclusivement. `Ioc.GetRequiredService<T>()` en dernier recours (vue créée par un gabarit, code WinUI).
* Pages et VM en **singleton** (application mono-fenêtre).

---

## Bibliothèques clés

* **CommunityToolkit.Mvvm** — MVVM, messenger. **CommunityToolkit.Maui / .Markup** — `[BindableProperty]`, popups, `FileSaver`, snackbar.
* **CodeEdit.Maui** (préversion) — éditeur de code (`.UseCodeEditor()`). Exige `Microsoft.Maui.Controls` >= 10.0.101 et la projection Windows forcée par `WindowsSdkPackageVersion`.
* **AathifMahir.Maui.MauiIcons.Fluent** — icônes Fluent. **LiveChartsCore.SkiaSharpView.Maui** — graphiques des dashboards.
* **Microsoft.Data.SqlClient** — SQL Server. **EF Core + Roslyn** — langage LINQ.
* **Newtonsoft.Json** — fichiers JSON de l'application (`JsonFile`) ; **System.Text.Json** (source-gen) — fichiers `.schx`.

Toutes les versions sont dans `src/Directory.Packages.props` : un `PackageReference` n'indique jamais de `Version`.

---

## Standards de codage

### Principes généraux

* **Cible** : `net10.0-windows10.0.19041.0` (win-x64) pour l'application et `UI`, `net10.0` pour le reste. C# 14 (version par défaut du SDK), nullable activé.
* Réglages communs (nullable, analyse `latest-all`, analyseurs trim/AOT) dans `src/Directory.Build.props` ; un csproj ne garde que ce qui lui est propre.
* **Lisibilité** : code clair et auto-explicatif, préférer le simple au sophistiqué. **SOLID**, injection par constructeur.
* **Async/Await** obligatoire pour tout I/O (disque, SecureStorage, base).
* **Magic numbers** : constantes ou tokens XAML.
* **Un type par fichier**, nommé comme lui, namespaces **file-scoped**. Seule exception : une classe **privée imbriquée**.
* Fichiers de l'application : `JsonFile` (écriture atomique, fichier corrompu mis de côté). Fichiers de l'utilisateur : `IEditorFileService` (encodage d'origine conservé, écriture atomique).

### Conventions de nommage

* **Classes & interfaces** : PascalCase (`IConnectionStore`). **Méthodes & propriétés** : PascalCase, suffixe `Async`.
* **Champs privés** : `_camelCase` ; constantes et `static readonly` : PascalCase. **Paramètres** : camelCase.
* **Pages** : suffixe `Page`. **ViewModels** : suffixe `VM`. **Contrôles** : nom du rôle sans préfixe.
* Règles reprises dans `src/.editorconfig`.

### Style de code

* **Accolades** : Allman. **Indentation** : 4 espaces en C#, un attribut par ligne en XAML.
* **Usings** : hors du namespace, inutilisés supprimés.

### BindableProperty

Générées par l'attribut `[BindableProperty]` de CommunityToolkit.Maui (classe `partial`, `MCTEXP001` neutralisé) : on déclare la propriété `partial`, le champ `XxxProperty` est généré.

```csharp
/// <summary>Bascule l'affichage des numeros de ligne.</summary>
[BindableProperty]
public partial ICommand? ToggleLineNumbersCommand { get; set; }

[BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnLanguageChanged))]
public partial SyntaxLanguage Language { get; set; } = SyntaxLanguage.Sql;
```

* Valeur par défaut = initialiseur. Options via l'attribut, toujours avec `nameof(...)` vers une méthode statique (pas de lambda).
* `{ get; private set; }` = propriété en lecture seule. `new partial` pour masquer une propriété héritée.
* Un `propertyChanged` n'est qu'un **dernier recours** : préférer un binding XAML.
* Contrôle riche : propriétés dans un fichier partiel `X.bindable.cs`.

### Conventions XAML

* **Namespaces implicites** (`MauiAllowImplicitXmlnsDeclaration`) : préfixes `c:`, `v:`, `vm:`, `m:`, `en:`, `inp:`, `conv:`, `beh:`, `mk:`, `mi:` sans déclaration, via `Properties/AppXmlns.cs` et `CoreXmlns.cs` (générés par T4).
* **Le T4 ne tourne pas au build** : après l'ajout ou la suppression d'un dossier de namespace, mettre `AppXmlns.cs` à jour.
* **Bindings compilés** (`MauiStrictXamlCompilation`) : toujours un `x:DataType`.
* **Contrôles en XAML + code-behind** : tout contrôle visuel est un couple `X.xaml` / `X.xaml.cs`. Structure, bindings internes, triggers et gestes dans le XAML ; le code-behind ne garde que les `[BindableProperty]`, `InitializeComponent()` et ce que le XAML ne sait pas faire (colonnes connues à l'exécution, natif WinUI), construit dans un conteneur nommé. Exceptions : non-vues et `UI.Inputs`.
* **Un contrôle ne connaît pas les ViewModels** : `BindableProperty` + liaison à soi-même (`x:Name="Root"` / `RelativeSource AncestorType`). La page hôte branche le VM.
* **Menus** : déclarés dans `MainPage.xaml` (`MenuBarItems`), liés à `MainVM` (Fichier) et `MainMenuBarVM` (le reste). Entrée cochable : `v:CheckedMenuFlyoutItem` ; liste : `v:MenuFlyoutSubItemExtended`.
* **Boutons** : `Button` avec styles `PrimaryButton`, `SecondaryButton`, `DangerButton`, `TextButton` ; `inp:ButtonView` pour un contenu personnalisé.
* **Icônes** : `{icons:Fluent Icon=Xxx}` / `{mk:FluentFilledImage Icon=Xxx}`.
* **Styles et couleurs** : tokens (`Resources/Styles/Tokens/`), jamais de couleur en dur.

### Langue

* Commentaires : français, succincts — le *pourquoi*, jamais le *quoi*.
* Noms de classes/méthodes : anglais. Textes d'interface, messages de commit, documentation : français.

---

## Directives d'implémentation

* Solutions simples et pragmatiques, pas de sur-ingénierie. Code prêt pour la production : pas de TODO.
* **Le moins de code-behind possible**. Un handler d'événement se justifie quand la bibliothèque tierce n'expose pas de binding (`CodeEditor`).
* Code spécifique plateforme : `Platforms/` ou gardes `#if WINDOWS`.
* Analyseurs trim/AOT actifs : éviter la réflexion non annotée.

---

## Prise de décision

1. **Intention** — quelle est la feature exacte ?
2. **Modèles** — `Core/Models/[Domaine]/` (ou `Databases/Models/` s'ils parlent au moteur).
3. **Service** — `Services/[Domaine]/` derrière une interface.
4. **ViewModel** — `ViewModels/Pages/` (`AbstractVM`) ou `Pages/Documents/` (`DocumentVM<TSession>`).
5. **Vue** — page dans `Views/Pages/` (`AbstractPage`, `PAGE_ROUTE`, `ShellContent` dans `AppShell.xaml`) ou contrôle dans `Views/Controls/[Domaine]/` ; générique → `SqlToolManagerMaui.UI`.
6. **DI** — `[AutoRegister]`. **Namespaces XAML** — `AppXmlns.cs`.
7. **Tests** — logique sans MAUI testable dans `SqlToolManagerMaui.Tests`.
8. **Compilation & validation**.

---

## Points clés spécifiques à ce projet

### Shell et fenêtre

* `AppShell` n'a qu'un `ShellContent` (`MainPage`) et `FlyoutBehavior="Disabled"` : pas de volet de navigation.
* La barre de menus est rendue par MAUI dans le `MauiToolbar` du Shell : la page doit porter `Shell.NavBarIsVisible="True"` (cf. dotnet/maui#6303).
* Fermeture de la fenêtre : `WindowCloseGuard` (WinUI `AppWindow.Closing`) appelle `MainVM.CanCloseAsync` (confirmation des onglets modifiés si la restauration des onglets est désactivée), puis `Destroying` mémorise les onglets et écrit les préférences en attente.

### Onglets (sessions)

* Un onglet = une `AbstractSession` (`Core/Models/Editors/`) autour d'un `EditorDocument` (texte enregistré / édité, chemin, langage, sélection, `IsDirty`). `CodeEditorSession` garde le résultat de la dernière exécution ; `DashboardSession` la vue, la définition, le fil d'Ariane et la lecture des cartes. `AbstractSession.LifetimeToken` est annulé à la fermeture de l'onglet.
* `MainVM` porte `Sessions`, `SelectedSession` et les commandes fichier. Fermer un onglet modifié passe par `IDialogService.AskSaveChangesAsync`.
* `CodeEditorVM` et `DashboardVM` (`DocumentVM<TSession>`) sont des **singletons** partagés par tous les onglets de leur type : la vue leur pousse `Session` (null quand aucun onglet de ce type n'est affiché). Aucun état propre à un onglet dans le VM.
* **Vues** : `DocumentViewSelector` (un `ViewSelector`, pas un `DataTemplateSelector`) réaffiche une seule instance de `CodeEditorView` / `DashboardView` d'un onglet à l'autre ; la vue transmet la session reçue (`Session="{Binding .}"`) à son VM.
* `MainVM.OnSelectedSessionChanged` appelle `DatabaseConnectionVM.AttachSession` (le sélecteur affiche la connexion de l'onglet, sans lever `SelectedConnectionChanged`) puis `MainMenuBarVM.AttachSession` (menus Édition, Requête et Langage branchés sur le VM du type de la session). `SelectedConnectionChanged` n'est levé que quand l'utilisateur change la connexion de l'onglet affiché.
* Le `CodeEditor` notifie par **événements** (`ContentChanged`, `SelectionChanged`, `DocumentChanged`), relayés par son code-behind.
* Fichiers : ouverture par `FilePicker`, « Enregistrer sous » par `FileSaver`. L'encodage du fichier est détecté (BOM, UTF-8, sinon ANSI Windows-1252) et conservé ; un nouveau fichier est en UTF-8 avec BOM.

### Exécution des requêtes

* `CodeEditorVM.ExecuteQueryCommand` (async, `IncludeCancelCommand`) : sélection si non vide, sinon tout le document. Volontairement **hors** `RunAsBusyAsync`, qui griserait la page et donc le bouton Annuler.
* Exécution via `IQueryExecutorResolver.Resolve(Document.Language)`.
* **Ajouter un moteur** = une classe `[AutoRegister(As = typeof(ISqlProviderExecutor))]` dans `Languages.Sql/[Moteur]/`.
* `SqlServerQueryExecutor` : une connexion par exécution, `CommandTimeout = 0`, lots `GO` / `GO n` (`SqlBatchSplitter`, qui ignore les GO dans chaînes et commentaires bloc), un lot en erreur n'arrête pas les suivants, lignes d'erreur recalées sur le script, erreurs SQL rendues dans les messages.
* Plafond de `QueryExecutionLimits.MaxRowsPerResultSet` (10 000) lignes par jeu, cellules converties en texte (NULL = `null`).
* **LINQ** : le script devient le corps d'une méthode générée (`LinqCompiler.BuildQuerySource`), le contexte porte le nom de la base. Le premier `return` termine le script ; `.Dump("titre")` ajoute un jeu de résultats. Chaque requête est chargée dans un `AssemblyLoadContext` **collectable**, déchargé après l'exécution (`LinqCompiledQuery.Dispose`). Toute exception du script est rendue dans les messages.
* **Limite LINQ** : le script tourne dans le processus de l'application. Annuler rend la main tout de suite, mais une boucle infinie continue en arrière-plan jusqu'à la fermeture, et un débordement de pile arrête l'application. Isoler l'exécution dans un processus séparé serait la seule vraie protection.
* `QueryResultGrid` : squelette XAML, en-tête et gabarit de ligne posés en code, lignes remplies au recyclage.

### Dashboards (`.schx`)

* Le **JSON reste la source** : le concepteur écrit dans la définition, la session lève `DefinitionChanged` et `DashboardVM` resérialise dans `EditorText` (regroupé sur 250 ms). La vue Source est relue au retour. F5 / Exécuter applique les paramètres et relit les cartes.
* `DashboardVM` est découpé en `DashboardVM.cs` (bascule des vues), `.Cards.cs` (consultation, lecture des cartes) et `.Designer.cs` (concepteur).
* **Format** (`Core/Models/Dashboards/DashboardDefinition`) : `connection` (nom d'une connexion enregistrée, jamais la chaîne), `database`, `columns` (12) / `rows` (6 minimum), `parameters` (`name`, `label`, `type` : Text, Integer, Decimal, Date, DateTime, Boolean, Guid, `default`), `startPage`, `pages` (`id`, `title`, `cards`). Carte : `column` / `row` (base 0), `columnSpan` / `rowSpan`, `view` (Grid, Value, BarChart, LineChart, PieChart, Text, Button), `query`, `text`, `labelColumn`, `valueColumns`, `navigation` (`page` + `parameters`).
* Les listes des modèles `.schx` sont en `{ get; set; } = []` et **jamais** `init` : le générateur System.Text.Json remet à null une propriété `init` absente du JSON.
* **Paramètres** : envoyés en paramètres typés (`DashboardParameterValue.TryGetQueryParameter`), jamais concaténés. Chaque étape du fil d'Ariane garde ses valeurs et ses résultats.
* **Lecture d'une carte** : premier jeu ayant des colonnes, 1 000 lignes max, cartes lues en parallèle ; une nouvelle lecture annule la précédente ; changer de connexion relit la page affichée.
* **Rendu** : `DashboardView` assemble `DashboardGrid`, `DashboardCardView`, `DashboardChartView` (LiveCharts2), `DashboardDesignerPanel`.
* Ajouter un moteur = aussi une implémentation de `IQueryReader` dans `Databases/[Moteur]/`, enregistrée dans `AddDatabases()`.

### Connexions

* Stockage mixte : métadonnées dans `connections.json` (`AppDataDirectory`, `JsonFile`), chaîne complète chiffrée dans le `SecureStorage` (`connectionstring:{id}`). `DatabaseConnectionInfo.ConnectionString` n'est renseignée qu'**en mémoire** (connexion ouverte) et n'est jamais sérialisée.
* Fichier corrompu : copie `.corrupt-date` puis liste vide. Fichier illisible : rien n'est mis en cache et toute écriture est refusée, pour ne pas écraser les connexions.
* Abonnement : `IConnectionStore.Subscribe` (appelé tout de suite puis à chaque ajout / modification / suppression) ; `DatabaseConnectionVM` et `ConnectionExplorerVM` s'y abonnent.
* **Ajouter un moteur** = une implémentation de `IConnectionTester` et de `IDatabaseSchemaReader` dans `Databases/[Moteur]/`, enregistrées dans `AddDatabases()`.
* La popup « Se connecter » (`ConnectionEditorPopup` + `ConnectionEditorVM`, transient) travaille sur un `DatabaseConnectionDraft` (clone) ; le mot de passe y est masqué (`ConnectionStringPasswordHelper`) et réinjecté tant que le masque n'est pas remplacé.
* **Explorateur** : `ConnectionExplorerVM` garde l'arbre, affiché par le contrôle générique `v:TreeView` (chargement paresseux par `ExpandingCommand`). Aucun `ExplorerNode` ne porte la chaîne. Suppression confirmée par `IDialogService.ConfirmAsync`.

### Préférences utilisateur

* `UserPreferences` (`Core/Models/Settings/`) : fichiers récents (10 max), restauration des onglets, options de l'éditeur (`Editor`), volets (`Panels`).
* Persistées par `IUserPreferencesStore` dans `preferences.json` : écriture regroupée (300 ms), jamais une version plus ancienne par-dessus une plus récente, `Flush()` à la fermeture. `PreferencesChanged` est un `event`.
* Onglets mémorisés : `IWorkspaceStore` (`workspace.json`), écrits à la fermeture, relus par `MainVM.LoadDatasBusyAsync`.
* Dossier nommé `Settings` (et non `Preferences`) pour ne pas masquer `Microsoft.Maui.Storage.Preferences`.

### Identité de l'application

* `ApplicationTitle` = Querify. `ApplicationId` (`fr.ichisamafr.querify`) nomme le dossier de données `%LOCALAPPDATA%\User Name\<ApplicationId>` (Data, Settings, coffre).
* Changer l'`ApplicationId` déplace ce dossier : renseigner l'ancien dans `MauiProgram.PreviousApplicationId`, `AppDataMigration` le recopie au premier lancement (l'ancien reste en place).

### Dépendances fragiles

* **CodeEdit.Maui / RichEdit.Maui** sont en préversion : une mise à jour peut casser l'éditeur. Ils imposent `Microsoft.Maui.Controls` >= 10.0.101 et la projection Windows `WindowsSdkPackageVersion` 10.0.19041.87 (sinon `FileNotFoundException` WinRT.Runtime au démarrage). Monter leur version seule, puis tester ouverture, coloration, complétion et sélection.
* L'usage de CodeEdit est regroupé dans `Views/Controls/Editors/` (`CodeEditor`, contrôleurs de complétion et de coloration) ; les VM n'en utilisent que `CodeEditorCommands`.

### Pièges connus

* Les `XmlnsDefinition` de `SqlToolManagerMaui.UI` ne sont vues que si son assembly est chargé **avant** le premier parsing XAML : si un converter devient introuvable au démarrage, forcer ce chargement dans `MauiProgram` (`typeof(...)` d'un type de UI).
* En MAUI 10, `DisplayAlert` / `DisplayActionSheet` sont obsolètes : `DisplayAlertAsync` / `DisplayActionSheetAsync`.
* Les `ResourceDictionary` sans `x:Class` sont analysés à l'exécution : une erreur de clé ne se voit pas à la compilation.

---

## Anti-patterns à éviter

* Ne PAS mettre de logique métier ou de navigation dans le code-behind des pages.
* Ne PAS référencer un ViewModel depuis un contrôle : passer par des `BindableProperty`.
* Ne PAS construire en C# l'arborescence visuelle d'un contrôle (ni des menus) quand un `.xaml` peut la porter.
* Ne PAS utiliser un `propertyChanged` là où un binding XAML suffit.
* Ne PAS accéder au conteneur via `Ioc` quand l'injection par constructeur est possible.
* Ne PAS appeler `Shell.Current` / `DisplayAlertAsync` depuis un VM : passer par `IDialogService`.
* Ne PAS écrire la chaîne de connexion complète en clair (JSON, journaux, modèle affichable).
* Ne PAS lancer une tâche avec `_ = XAsync()` : `.FireAndForget("message")`.
* Ne PAS avaler une exception sans `LogError`, ni utiliser `ILogger`.
* Ne PAS indiquer de `Version` dans un `PackageReference` (`Directory.Packages.props`).
* Ne PAS ignorer async/await pour les accès disque, SecureStorage ou base.
* Ne PAS utiliser les API Xamarin.Forms.
* Ne PAS déclarer plusieurs types dans un même fichier — sauf une classe privée imbriquée.

---

## Validation

```bash
dotnet build src/SqlToolManagerMaui.slnx
dotnet test src/SqlToolManagerMaui.Tests/SqlToolManagerMaui.Tests.csproj
```

`MauiStrictXamlCompilation` transforme les avertissements de binding en erreurs : un `x:DataType` manquant casse le build. La CI (`.github/workflows/build.yml`) fait la même chose sur Windows à chaque push et pull request.

---

**Dernière mise à jour** : 2026-10-20

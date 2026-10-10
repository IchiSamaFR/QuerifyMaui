# SqlToolManagerMaui - Instructions pour Claude

## 🎯 À propos du projet

SqlToolManagerMaui est une application desktop Windows .NET MAUI pour écrire et exécuter des requêtes SQL / EntityFramework : éditeur multi-onglets avec coloration syntaxique, gestion des connexions aux bases, habillage Windows 11. Architecture MVVM basée sur CommunityToolkit.Mvvm.

---

## 📋 Source centralisée

**Les instructions complètes et à jour se trouvent dans** : `.github/instructions.md`

Cette source unique est partagée entre Claude et Copilot. Consulte ce fichier pour :

- ✅ Contexte fonctionnel complet
- ✅ Architecture et structure du dépôt
- ✅ Pattern MVVM et injection de dépendances
- ✅ Bibliothèques clés
- ✅ Standards de codage, conventions de nommage et conventions XAML
- ✅ Directives d'implémentation
- ✅ Prise de décision et anti-patterns
- ✅ Validation et compilation

---

## 🔑 Points clés à retenir

**Avant chaque discussion, je consulte** `.github/instructions.md` pour m'assurer que je suis à jour.

Respecter **absolument** :
1. L'architecture MVVM — les ViewModels héritent de `AbstractVM`, les pages de `AbstractPage`
2. La stratification des couches — Views, ViewModels, Services (app), `SqlToolManagerMaui.Core` (modèles, enums, contrats applicatifs), `SqlToolManagerMaui.Databases` (moteurs de base : connexion, catalogue, résolveurs), `SqlToolManagerMaui.Languages.Commons` (contrats communs aux langages), `SqlToolManagerMaui.Languages.[Nom]` (une implémentation par langage), `SqlToolManagerMaui.Languages` (agrège les langages et fournit les résolveurs) et `SqlToolManagerMaui.UI` pour le générique
3. L'injection de dépendances — déclaration par `[AutoRegister]` sur la classe (scan dans `Ioc/ServiceCollectionExtensions.cs`), injection par constructeur
4. Les anti-patterns à éviter — pas de logique dans le code-behind, pas de ViewModel connu d'un contrôle, pas d'API Xamarin.Forms

---

## 🧩 BindableProperty

Les `BindableProperty` sont générées par l'attribut `[BindableProperty]` de CommunityToolkit.Maui, sur une propriété `partial` (classe `partial`) :

```csharp
/// <summary>Bascule l'affichage des numeros de ligne.</summary>
[BindableProperty]
public partial ICommand? ToggleLineNumbersCommand { get; set; }

[BindableProperty(DefaultBindingMode = BindingMode.TwoWay, PropertyChangedMethodName = nameof(OnSelectedItemChanged))]
public partial EditorDocument? SelectedItem { get; set; }
```

Valeur par défaut = initialiseur ; callbacks = méthodes statiques référencées par `nameof` (pas de lambda) ; `private set` = propriété en lecture seule.
Un `propertyChanged` est un dernier recours : le binding XAML d'abord.

---

## ⚙️ Outils et ressources

- **Source centralisée** : `.github/instructions.md`
- **Référence Copilot** : `.github/copilot-instructions.md`
- **Repository** : SqlToolManagerMaui
- **Framework** : .NET MAUI (net10.0-windows10.0.19041.0, win-x64)
- **Solution** : `src/SqlToolManagerMaui.slnx`

---

## ❌ Anti-patterns à éviter

- Ne PAS mettre de logique métier ou de navigation dans le code-behind des pages
- Ne PAS référencer un ViewModel depuis un contrôle — passer par des `BindableProperty`
- Ne PAS utiliser un `propertyChanged` là où un binding XAML suffit
- Ne PAS accéder au conteneur via `Ioc` quand l'injection par constructeur est possible
- Ne PAS écrire la chaîne de connexion complète en clair
- Ne PAS ignorer async/await pour les accès disque, SecureStorage ou base
- Ne PAS utiliser les API Xamarin.Forms — toujours MAUI
- Ne PAS contourner `AbstractVM` / `AbstractPage` pour une nouvelle page
- Ne PAS construire en C# l'arborescence visuelle d'un contrôle quand un `.xaml` peut la porter

---

## 💬 Commentaires dans le code

- Rester **succinct** : un commentaire = une ligne courte, sauf nécessité réelle d'expliquer un choix non évident
- Ne PAS commenter ce que le code dit déjà clairement (pas de paraphrase)
- Pas de blocs de commentaires longs au-dessus des méthodes ou des classes
- Commenter uniquement le **pourquoi** (raison métier, contournement, piège) — jamais le **quoi**
- Pas de commentaires redondants entre XAML/code-behind/ViewModel pour la même logique
- Commentaires en français

---

## ✅ Processus courants

### Créer une nouvelle page
1. Modèles dans `Core/Models/[Domaine]/`, contrat partagé dans `Core/Services/[Domaine]/`, service dans `Services/[Domaine]/` (ou dans le projet de langage concerné) derrière une interface
2. ViewModel dans `ViewModels/Pages/` (hérite de `AbstractVM`)
3. Page XAML dans `Views/Pages/` (hérite de `AbstractPage`, expose `PAGE_ROUTE`)
4. `ShellContent` dans `AppShell.xaml` PUIS `c:NavigationRailItem` dans le rail
5. Marquer service, page et VM avec `[AutoRegister]` (`As = typeof(IService)` pour un service, `Lifetime = ServiceLifetime.Transient` si besoin)
6. Compiler : `dotnet build src/SqlToolManagerMaui.slnx`

### Extraire un contrôle
1. Spécifique à l'app → `Views/Controls/[Domaine]/` ; générique sans métier → `SqlToolManagerMaui.UI`
2. Toujours un couple `X.xaml` + `X.xaml.cs` (même pour un `MenuFlyoutItem` ou un `Grid`) : structure, bindings internes, triggers et gestes dans le XAML ; le code-behind se limite aux `[BindableProperty]`, à `InitializeComponent()` et à ce que le XAML ne sait pas faire (colonnes dynamiques, natif WinUI), construit dans un conteneur nommé du XAML. Exceptions : non-vues et `UI.Inputs` (classes de base dérivées)
3. Exposer des `BindableProperty`, jamais un `x:DataType` de ViewModel
4. Se lier à soi-même via `x:Name="Root"` + `{Binding MaProp, Source={x:Reference Root}, x:DataType=v:MonControle}` dans le contrôle, ou `{Binding Source={RelativeSource AncestorType={x:Type c:MonControle}}, ...}` dans un contrôle composé
5. Si un nouveau dossier de namespace est créé, mettre `Properties/AppXmlns.cs` à jour (le T4 ne tourne pas au build)

### Ajouter un moteur SQL
1. Dans `Databases/[Moteur]/` : classes `[AutoRegister(As = typeof(IConnectionTester))]`, `[AutoRegister(As = typeof(IDatabaseSchemaReader))]` et `[AutoRegister(As = typeof(IDashboardQueryReader))]` (les résolveurs les découvrent seuls)
2. Dans `Languages.Sql/[Moteur]/` : classe `[AutoRegister(As = typeof(ISqlProviderExecutor))]` (`SqlQueryExecutor` la découvre seul)
3. Ajouter la valeur à `DatabaseProvider` (`Core/Enums`)

### Ajouter un langage
1. Créer `SqlToolManagerMaui.Languages.[Nom]` (référence `Languages.Commons` uniquement, jamais un autre langage)
2. Y implémenter `IQueryExecutor` (`Languages.Commons`) en `[AutoRegister(As = typeof(IQueryExecutor))]`, avec sa valeur de `SyntaxLanguage` : `QueryExecutorResolver` le découvre seul
3. L'ajouter à la solution et en `ProjectReference` de `SqlToolManagerMaui.Languages` (jamais du projet principal)
4. Ajouter un de ses types à `LanguageAssemblies.All` (`SqlToolManagerMaui.Languages`) pour que son assembly soit scannée

---

**Dernière mise à jour** : 2026-10-10
**Source centralisée** : `.github/instructions.md`

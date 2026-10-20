# Copilot Instructions — SqlToolManagerMaui

**Source partagée avec Claude** : consultez `.github/instructions.md` pour le contexte complet et à jour.

Ce fichier fait référence aux instructions centralisées. Copilot doit toujours :

* Consulter les instructions en priorité et ne chercher via search/bash que si vous rencontrez des informations inattendues.
* Respecter l'architecture MVVM et la stratification des couches avant de générer du code.
* Éviter de casser la logique existante des pages déjà livrées.
* Ne jamais générer du code Xamarin.Forms — toujours utiliser les équivalents .NET MAUI.
* Déclarer les `BindableProperty` via l'attribut `[BindableProperty]` de CommunityToolkit.Maui sur une propriété `partial`, et préférer le binding XAML au code-behind.

Voir `.github/instructions.md` pour :
- Contexte fonctionnel complet
- Architecture et structure du dépôt
- Pattern MVVM et injection de dépendances
- Bibliothèques clés
- Standards de codage, conventions de nommage et conventions XAML
- Directives d'implémentation
- Prise de décision et anti-patterns
- Validation et compilation

---

**Dernière mise à jour** : 2026-10-20

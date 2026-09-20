using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors;

/// <summary>
/// Menu "Requete" : execution (F5) et deconnexion. Les commandes sont fournies par la page hote.
/// </summary>
public partial class QueryMenuBarItem : MenuBarItem
{
    public static readonly BindableProperty ExecuteCommandProperty = BindableProperty.Create(
        nameof(ExecuteCommand), typeof(ICommand), typeof(QueryMenuBarItem), null);

    /// <summary>Execute la requete active (F5).</summary>
    public ICommand? ExecuteCommand
    {
        get => (ICommand?)GetValue(ExecuteCommandProperty);
        set => SetValue(ExecuteCommandProperty, value);
    }

    public static readonly BindableProperty DisconnectCommandProperty = BindableProperty.Create(
        nameof(DisconnectCommand), typeof(ICommand), typeof(QueryMenuBarItem), null);

    /// <summary>Ferme la connexion courante.</summary>
    public ICommand? DisconnectCommand
    {
        get => (ICommand?)GetValue(DisconnectCommandProperty);
        set => SetValue(DisconnectCommandProperty, value);
    }

    public QueryMenuBarItem()
    {
        InitializeComponent();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.UI.Views.Controls.Menus;
using SqlToolManagerMaui.Views.Controls.Editors.Menus;
using System.Windows.Input;

namespace SqlToolManagerMaui.Services.Contexts;

public abstract class AbstractMenuContext : ObservableObject
{
    public abstract MenuBarItem CreateMenu();

    #region Construeurs
    protected IMenuFlyoutItem CreateSeparator()
    {
        return new MenuFlyoutSeparator();
    }

    protected IMenuFlyoutItem CreateItem(string text, ICommand? command, object commandParameter = null)
    {
        return new MenuFlyoutItem()
        {
            Text = text,
            Command = command,
            CommandParameter = commandParameter
        };
    }

    /// <summary>Entree dont la commande est liee a <paramref name="commandPath"/> : elle suit les reaffectations du contexte.</summary>
    protected IMenuFlyoutItem CreateBoundItem(string text, string commandPath)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.SetBinding(MenuItem.CommandProperty, new Binding(commandPath, BindingMode.OneWay, source: this));
        return item;
    }

    protected IMenuFlyoutItem CreateSubItem(string text,
        string emptyText,
        IEnumerable<string> itemSource,
        ICommand itemCommand,
        ICommand footerCommand)
    {
        return new MenuFlyoutSubItemExtended()
        {
            Text = text,
            EmptyText = emptyText,
            ItemsSource = itemSource,
            ItemCommand = itemCommand,
            FooterCommand = footerCommand
        };
    }

    protected MenuBarItem CreateItem(string text, IList<IMenuFlyoutItem> list)
    {
        var item = new MenuBarItem
        {
            Text = text
        };
        foreach (var menu in list)
        {
            item.Add(menu);
        }
        return item;
    }

    protected IMenuFlyoutItem CreateSubMenu(string text, IList<IMenuFlyoutItem> list)
    {
        var item = new MenuFlyoutSubItem
        {
            Text = text
        };
        foreach (var menu in list)
        {
            item.Add(menu);
        }
        return item;
    }

    /// <summary>Entree cochable : <paramref name="isCheckedPath"/> est lie sur <paramref name="source"/> (doit notifier).</summary>
    protected IMenuFlyoutItem CreateCheckedItem(string text,
        ICommand command,
        object source,
        string isCheckedPath,
        IValueConverter? converter = null,
        object? converterParameter = null,
        object? commandParameter = null)
    {
        var item = new CheckedMenuFlyoutItem
        {
            Label = text,
            Command = command,
            CommandParameter = commandParameter
        };
        item.SetBinding(CheckedMenuFlyoutItem.IsCheckedProperty,
            new Binding(isCheckedPath, BindingMode.OneWay, converter, converterParameter, source: source));
        return item;
    }
    #endregion
}

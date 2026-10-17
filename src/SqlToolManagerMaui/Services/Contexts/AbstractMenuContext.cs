using CommunityToolkit.Mvvm.ComponentModel;
using SqlToolManagerMaui.UI.Models.Keyboards;
using SqlToolManagerMaui.UI.Views.Controls.Menus;
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

    protected IMenuFlyoutItem CreateBoundItem(string text, string commandPath, KeyboardShortcut shortcut)
    {
        var item = CreateBoundItem(text, commandPath) as MenuFlyoutItem;
        item!.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = shortcut.Key, Modifiers = shortcut.Modifiers });
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

    /// <summary>Sous-menu alimente par une liste : source et commandes sont liees aux chemins du contexte.</summary>
    protected IMenuFlyoutItem CreateBoundSubItem(string text,
        string emptyText,
        string itemsSourcePath,
        string itemCommandPath,
        string footerCommandPath)
    {
        var item = new MenuFlyoutSubItemExtended
        {
            Text = text,
            EmptyText = emptyText
        };
        item.SetBinding(MenuFlyoutSubItemExtended.ItemsSourceProperty, new Binding(itemsSourcePath, BindingMode.OneWay, source: this));
        item.SetBinding(MenuFlyoutSubItemExtended.ItemCommandProperty, new Binding(itemCommandPath, BindingMode.OneWay, source: this));
        item.SetBinding(MenuFlyoutSubItemExtended.FooterCommandProperty, new Binding(footerCommandPath, BindingMode.OneWay, source: this));
        return item;
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

    /// <summary>Entree cochable dont la commande et l'etat sont lies aux chemins du contexte.</summary>
    protected IMenuFlyoutItem CreateBoundCheckedItem(string text,
        string commandPath,
        string isCheckedPath,
        IValueConverter? converter = null,
        object? converterParameter = null,
        object? commandParameter = null)
    {
        var item = new CheckedMenuFlyoutItem
        {
            Label = text,
            CommandParameter = commandParameter
        };
        item.SetBinding(MenuItem.CommandProperty, new Binding(commandPath, BindingMode.OneWay, source: this));
        item.SetBinding(CheckedMenuFlyoutItem.IsCheckedProperty,
            new Binding(isCheckedPath, BindingMode.OneWay, converter, converterParameter, source: this));
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

using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.Views.Controls.Editors.Menus;

/// <summary>
/// Menu "Edition" d'un editeur. Les commandes ne viennent pas d'un ViewModel mais du
/// controle <see cref="CodeEditor"/> lui-meme : la page hote injecte l'editeur via
/// <see cref="Editor"/>, le XAML se branche sur CodeEditor.Commands.
/// </summary>
public partial class EditMenuBarItem : MenuBarItem
{
    /// <summary>Editeur pilote par ce menu.</summary>
    [BindableProperty]
    public partial CodeEditor? Editor { get; set; }

    public EditMenuBarItem()
    {
        InitializeComponent();
    }
}

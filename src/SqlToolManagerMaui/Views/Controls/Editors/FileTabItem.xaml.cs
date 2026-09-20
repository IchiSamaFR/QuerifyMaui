using SqlToolManagerMaui.Models.Editors;
using System.Windows.Input;

namespace SqlToolManagerMaui.Views.Controls.Editors;

public partial class FileTabItem : ContentView
{
	public static readonly BindableProperty CloseCommandProperty = BindableProperty.Create(
        nameof(CloseCommand),
        typeof(ICommand),
        typeof(FileTabItem),
        null);

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public static readonly BindableProperty DocumentProperty = BindableProperty.Create(
        nameof(Document),
        typeof(EditorDocument),
        typeof(FileTabItem),
        null);

    public EditorDocument? Document
    {
        get => (EditorDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public FileTabItem()
	{
		InitializeComponent();
	}
}
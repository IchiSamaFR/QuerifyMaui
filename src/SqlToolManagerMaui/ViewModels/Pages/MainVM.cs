using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SqlToolManagerMaui.ViewModels.Pages;

public partial class MainVM : AbstractVM
{
    [ObservableProperty]
    public partial int Counter { get; set; }

    [RelayCommand]
    private void AddCounter()
    {
        Counter++;
    }
}

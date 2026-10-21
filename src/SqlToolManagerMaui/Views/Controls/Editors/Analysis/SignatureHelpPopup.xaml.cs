using CommunityToolkit.Maui;

namespace SqlToolManagerMaui.Views.Controls.Editors.Analysis;

/// <summary>Signature de la methode appelee : <see cref="Before"/> + parametre courant (<see cref="Active"/>) + <see cref="After"/>.</summary>
public partial class SignatureHelpPopup : ContentView
{
    public const double MaxPopupWidth = 640;

    [BindableProperty]
    public partial string? Before { get; set; }

    [BindableProperty]
    public partial string? Active { get; set; }

    [BindableProperty]
    public partial string? After { get; set; }

    /// <summary>"2/4" quand la methode a plusieurs surcharges ; null sinon.</summary>
    [BindableProperty]
    public partial string? Counter { get; set; }

    [BindableProperty]
    public partial string? Description { get; set; }

    public SignatureHelpPopup()
    {
        InitializeComponent();
    }
}

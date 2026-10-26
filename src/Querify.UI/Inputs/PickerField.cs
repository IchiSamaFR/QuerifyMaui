using Querify.UI.Inputs.Core;
using System.Collections;
using System.Reflection;

namespace Querify.UI.Inputs;

/// <summary>
/// Champ de sélection (Picker) avec titre flottant, icône et validations.
///
/// <see cref="ItemsSource"/> accepte au choix :
///   • un dictionnaire (<see cref="IDictionary"/>) : chaque entrée expose sa clé et sa valeur ;
///   • une liste simple (<see cref="IEnumerable"/>).
///
/// Le libellé affiché est résolu via <see cref="DisplayMemberPath"/> (chemin de propriété,
/// ex. "Name" ou "User.Name") appliqué à la valeur ; à défaut <c>ToString()</c>.
///
/// La sélection est exposée sous trois formes synchronisées :
///   • <see cref="SelectedItem"/> — l'élément source (KeyValuePair pour un dictionnaire, l'objet pour une liste) ;
///   • <see cref="SelectedValue"/> — la valeur (valeur du dictionnaire, ou l'élément pour une liste) ;
///   • <see cref="SelectedKey"/> — la clé (clé du dictionnaire, <c>null</c> pour une liste).
/// </summary>
[ContentProperty(nameof(Validations))]
public partial class PickerField : InputField
{
    private bool _isSyncing;
    private readonly List<PickerEntry> _entries = new();

    private readonly BorderlessPicker _pickerView = new BorderlessPicker
    {
        Margin = new Thickness(0, 0),
        BackgroundColor = Colors.Transparent,
        VerticalOptions = LayoutOptions.Center,
        HorizontalOptions = LayoutOptions.Fill,
    };

    public BorderlessPicker PickerView => _pickerView;

    public override View Content { get; set; }

    public override bool HasValue => _pickerView.SelectedIndex >= 0;

    public event EventHandler? SelectionChanged;

    public PickerField()
    {
        Content = PickerView;
        RegisterForEvents();

        PickerView.ItemDisplayBinding = new Binding(nameof(PickerEntry.Label));
        PickerView.SelectedIndexChanged += OnPickerSelectedIndexChanged;

        PickerView.SetBinding(Picker.MarginProperty, new Binding(nameof(ContentPadding), source: this));
        PickerView.SetBinding(Picker.TextColorProperty, new Binding(nameof(TextColor), source: this));
        PickerView.SetBinding(Picker.TitleProperty, new Binding(nameof(Title), source: this));
        PickerView.SetBinding(Picker.TitleColorProperty, new Binding(nameof(TitleColor), source: this));
        PickerView.SetBinding(Picker.HorizontalTextAlignmentProperty, new Binding(nameof(HorizontalTextAlignment), source: this));

        PickerView.SetBinding(Picker.FontSizeProperty, new Binding(nameof(FontSize), source: this));
        PickerView.SetBinding(Picker.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));

        RebuildEntries();
    }

    // ── Construction de la liste interne ──────────────────────────────────────

    private void RebuildEntries()
    {
        _entries.Clear();

        switch (ItemsSource)
        {
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                    _entries.Add(new PickerEntry
                    {
                        Key = entry.Key,
                        Value = entry.Value,
                        Item = new KeyValuePair<object?, object?>(entry.Key, entry.Value),
                        Label = ResolveLabel(entry.Value),
                    });
                break;

            case IEnumerable enumerable and not string:
                foreach (var item in enumerable)
                    _entries.Add(new PickerEntry
                    {
                        Key = null,
                        Value = item,
                        Item = item,
                        Label = ResolveLabel(item),
                    });
                break;
        }

        // Réaffecte la source pour forcer le Picker à relire les libellés.
        _pickerView.ItemsSource = null;
        _pickerView.ItemsSource = _entries;

        RestoreSelection();
    }

    private string ResolveLabel(object? source)
    {
        if (source is null)
            return string.Empty;

        if (string.IsNullOrEmpty(DisplayMemberPath))
            return source.ToString() ?? string.Empty;

        return ResolvePath(source, DisplayMemberPath!)?.ToString() ?? string.Empty;
    }

    private static object? ResolvePath(object? obj, string path)
    {
        foreach (var member in path.Split('.'))
        {
            if (obj is null)
                return null;

            var property = obj.GetType().GetProperty(member, BindingFlags.Public | BindingFlags.Instance);
            obj = property?.GetValue(obj);
        }

        return obj;
    }

    // ── Synchronisation de la sélection ───────────────────────────────────────

    private void OnPickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isSyncing)
            return;

        SelectEntry(_pickerView.SelectedItem as PickerEntry);
    }

    private void SelectEntry(PickerEntry? entry)
    {
        _isSyncing = true;

        _pickerView.SelectedItem = entry;
        SelectedItem = entry?.Item;
        SelectedValue = entry?.Value;
        SelectedKey = entry?.Key;

        _isSyncing = false;

        OnSelectionUpdated();
    }

    private void RestoreSelection()
    {
        var match =
            _entries.FirstOrDefault(x => Equals(x.Item, SelectedItem))
            ?? (SelectedKey is null ? null : _entries.FirstOrDefault(x => Equals(x.Key, SelectedKey)))
            ?? (SelectedValue is null ? null : _entries.FirstOrDefault(x => Equals(x.Value, SelectedValue)));

        _isSyncing = true;
        _pickerView.SelectedItem = match;
        _isSyncing = false;

        OnSelectionUpdated();
    }

    private void OnSelectionUpdated()
    {
        CheckAndShowValidations();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        UpdateState();
    }

    // Callbacks invoqués depuis les BindableProperty.
    private void OnItemsSourceChanged() => RebuildEntries();

    private void OnDisplayMemberPathChanged() => RebuildEntries();

    private void OnSelectedItemChanged()
    {
        if (_isSyncing)
            return;

        SelectEntry(_entries.FirstOrDefault(x => Equals(x.Item, SelectedItem)));
    }

    private void OnSelectedValueChanged()
    {
        if (_isSyncing)
            return;

        SelectEntry(_entries.FirstOrDefault(x => Equals(x.Value, SelectedValue)));
    }

    private void OnSelectedKeyChanged()
    {
        if (_isSyncing)
            return;

        SelectEntry(_entries.FirstOrDefault(x => Equals(x.Key, SelectedKey)));
    }

    protected override object? GetValueForValidator() => SelectedItem;

    public override void ResetValidation()
    {
        SelectEntry(null);
        base.ResetValidation();
    }

    /// <summary>Projection interne d'un élément de <see cref="ItemsSource"/> pour le Picker.</summary>
    private sealed class PickerEntry
    {
        public object? Key { get; init; }
        public object? Value { get; init; }
        public object? Item { get; init; }
        public string Label { get; init; } = string.Empty;
    }
}

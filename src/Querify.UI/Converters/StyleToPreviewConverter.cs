using System.Globalization;

namespace Querify.UI.Converters;

// Construit dynamiquement un control de prévisualisation à partir d'un Style.
// Réservé à la page de tests des styles : instancie le TargetType et applique le Style.
public class StyleToPreviewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Style style)
        {
            return null;
        }

        try
        {
            if (Activator.CreateInstance(style.TargetType) is not View view)
            {
                return BuildFallback($"{style.TargetType.Name} non prévisualisable");
            }

            view.Style = style;
            ApplySampleContent(view);
            return view;
        }
        catch (Exception ex)
        {
            return BuildFallback($"!!! {style.TargetType.Name} !!! : {ex.Message}");
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static void ApplySampleContent(View view)
    {
        TrySetString(view, "Text", "Exemple");
        TrySetString(view, "Placeholder", "Exemple");
        TrySetString(view, "Title", "Exemple");

        // Donne une dimension visible aux controls qui n'ont pas de contenu textuel.
        if (view is Image image)
        {
            image.HeightRequest = 64;
            image.WidthRequest = 64;
        }

        // Pour les layouts, injecte un enfant afin de matérialiser le style.
        if (view is Layout layout && layout.Children.Count == 0)
        {
            layout.Children.Add(new Label { Text = "Contenu" });
        }
    }

    private static void TrySetString(View view, string propertyName, string value)
    {
        try
        {
            var property = view.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite && property.PropertyType == typeof(string))
            {
                property.SetValue(view, value);
            }
        }
        catch
        {
            // Propriété non assignable sur ce control : on ignore.
        }
    }

    private static Label BuildFallback(string message) => new()
    {
        Text = message,
        FontAttributes = FontAttributes.Italic,
    };
}

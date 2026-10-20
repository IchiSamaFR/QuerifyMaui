using Microsoft.Maui.Layouts;

namespace SqlToolManagerMaui.UI.Views.Controls.Layouts;

/// <summary>
/// Dispose ses enfants de gauche a droite et passe a la ligne quand la largeur est atteinte,
/// avec un espacement fixe entre les elements (horizontal) et entre les lignes (vertical).
/// S'utilise avec BindableLayout.ItemsSource / ItemTemplate comme un ItemsControl.
/// </summary>
public class WrapLayout : Layout
{
    public static readonly BindableProperty HorizontalSpacingProperty =
        BindableProperty.Create(nameof(HorizontalSpacing), typeof(double), typeof(WrapLayout), 0d, propertyChanged: OnSpacingChanged);

    public static readonly BindableProperty VerticalSpacingProperty =
        BindableProperty.Create(nameof(VerticalSpacing), typeof(double), typeof(WrapLayout), 0d, propertyChanged: OnSpacingChanged);

    /// <summary>Espace entre deux elements d'une meme ligne.</summary>
    public double HorizontalSpacing
    {
        get => (double)GetValue(HorizontalSpacingProperty);
        set => SetValue(HorizontalSpacingProperty, value);
    }

    /// <summary>Espace entre deux lignes.</summary>
    public double VerticalSpacing
    {
        get => (double)GetValue(VerticalSpacingProperty);
        set => SetValue(VerticalSpacingProperty, value);
    }

    protected override ILayoutManager CreateLayoutManager() => new WrapLayoutManager(this);

    private static void OnSpacingChanged(BindableObject bindable, object oldValue, object newValue)
        => ((IView)bindable).InvalidateMeasure();

    private sealed class WrapLayoutManager : LayoutManager
    {
        private readonly WrapLayout _layout;

        public WrapLayoutManager(WrapLayout layout) : base(layout)
        {
            _layout = layout;
        }

        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var padding = _layout.Padding;
            var available = Math.Max(0, widthConstraint - padding.HorizontalThickness);

            foreach (var child in VisibleChildren())
                child.Measure(available, double.PositiveInfinity);

            var size = Flow(available, arrange: null);
            return new Size(size.Width + padding.HorizontalThickness, size.Height + padding.VerticalThickness);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var padding = _layout.Padding;
            var origin = new Point(bounds.X + padding.Left, bounds.Y + padding.Top);
            var available = Math.Max(0, bounds.Width - padding.HorizontalThickness);

            var size = Flow(available, (child, x, y) =>
                child.Arrange(new Rect(origin.X + x, origin.Y + y, child.DesiredSize.Width, child.DesiredSize.Height)));

            return new Size(size.Width + padding.HorizontalThickness, size.Height + padding.VerticalThickness);
        }

        /// <summary>Place les enfants deja mesures ligne par ligne ; renvoie la taille occupee.</summary>
        private Size Flow(double available, Action<IView, double, double>? arrange)
        {
            double x = 0, y = 0, rowHeight = 0, width = 0;
            var isRowEmpty = true;

            foreach (var child in VisibleChildren())
            {
                var desired = child.DesiredSize;

                // Nouvelle ligne si l'element ne tient plus (un element seul trop large garde sa ligne).
                if (!isRowEmpty && x + _layout.HorizontalSpacing + desired.Width > available)
                {
                    y += rowHeight + _layout.VerticalSpacing;
                    x = 0;
                    rowHeight = 0;
                    isRowEmpty = true;
                }

                if (!isRowEmpty)
                    x += _layout.HorizontalSpacing;

                arrange?.Invoke(child, x, y);

                x += desired.Width;
                rowHeight = Math.Max(rowHeight, desired.Height);
                width = Math.Max(width, x);
                isRowEmpty = false;
            }

            return new Size(width, y + rowHeight);
        }

        private IEnumerable<IView> VisibleChildren()
            => _layout.Where(child => child.Visibility != Visibility.Collapsed);
    }
}

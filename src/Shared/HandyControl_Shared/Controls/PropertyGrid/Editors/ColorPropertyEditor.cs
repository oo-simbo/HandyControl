using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HandyControl.Controls;

/// <summary>Common color palette editor for Color and solid Brush properties.</summary>
public class ColorPropertyEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyItem propertyItem) => new ColorPalette
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        IsEnabled = !propertyItem.IsReadOnly
    };

    public override DependencyProperty GetDependencyProperty() => ColorPalette.SelectedBrushProperty;

    protected override IValueConverter GetConverter(PropertyItem propertyItem) => new ColorBrushConverter();

    private sealed class ColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            Color color => new SolidColorBrush(color),
            SolidColorBrush brush => brush,
            _ => DependencyProperty.UnsetValue
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is SolidColorBrush brush
                ? targetType == typeof(Color) ? brush.Color : brush
                : Binding.DoNothing;
    }
}

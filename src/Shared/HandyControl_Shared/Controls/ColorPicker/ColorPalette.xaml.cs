using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using HandyControl.Tools;

namespace HandyControl.Controls;

/// <summary>Two rows of common colors with a preview opening the full ColorPicker.</summary>
public partial class ColorPalette : UserControl
{
    public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(
        nameof(SelectedBrush), typeof(SolidColorBrush), typeof(ColorPalette),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((ColorPalette) d).UpdatePreview()));

    public SolidColorBrush SelectedBrush
    {
        get => (SolidColorBrush) GetValue(SelectedBrushProperty);
        set => SetValue(SelectedBrushProperty, value);
    }

    private Popup? _pickerPopup;

    public ColorPalette()
    {
        InitializeComponent();
        string[] colors =
        [
            "#F44336", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5",
            "#2196F3", "#03A9F4", "#00BCD4", "#009688", "#000000",
            "#4CAF50", "#8BC34A", "#CDDC39", "#FFEB3B", "#FFC107",
            "#FF9800", "#FF5722", "#795548", "#9E9E9E", "#FFFFFF"
        ];
        foreach (var hex in colors)
        {
            var brush = new SolidColorBrush((Color) ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            var button = new Button
            {
                Background = brush, Tag = brush, ToolTip = hex,
                Height = 18, MinWidth = 6, Margin = new Thickness(1),
                Style = (Style) Resources["SwatchStyle"]
            };
            AutomationProperties.SetName(button, hex);
            button.Click += (_, _) => SetCurrentValue(SelectedBrushProperty, brush);
            Swatches.Children.Add(button);
        }
        Unloaded += (_, _) => ClosePicker();
        IsEnabledChanged += (_, _) => { if (!IsEnabled) ClosePicker(); };
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (CurrentColor == null) return;
        var color = SelectedBrush?.Color ?? Colors.Transparent;
        var hex = color.A == 255 ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : color.ToString();
        CurrentColor.Background = SelectedBrush;
        // Composite over white to keep the hex label readable for translucent colors.
        var alpha = color.A / 255d;
        var luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) * alpha + 255 * (1 - alpha);
        HexText.Foreground = luminance > 150 ? Brushes.Black : Brushes.White;
        HexText.Text = hex;
        // 按钮变窄后可见文本会被省略，完整 #RRGGBB / #AARRGGBB 保留在 ToolTip 与 Automation 名称里。
        CurrentColor.ToolTip = hex + " ColorPicker";
        AutomationProperties.SetName(CurrentColor, hex + " ColorPicker");
    }

    private void ClosePicker()
    {
        if (_pickerPopup != null) _pickerPopup.IsOpen = false;
    }

    private void OpenPicker(object sender, RoutedEventArgs e)
    {
        if (_pickerPopup != null) { ClosePicker(); return; }
        var picker = new ColorPicker
        {
            Style = ResourceHelper.GetResource<Style>("ColorPickerBaseStyle"),
            SelectedBrush = SelectedBrush?.CloneCurrentValue() ?? Brushes.Transparent
        };
        var popup = new Popup
        {
            PlacementTarget = CurrentColor, Placement = PlacementMode.Bottom,
            StaysOpen = false, AllowsTransparency = true, Child = picker
        };
        _pickerPopup = popup;
        var owner = System.Windows.Window.GetWindow(this);
        EventHandler deactivate = (_, _) => ClosePicker();
        if (owner != null) owner.Deactivated += deactivate;
        popup.PreviewKeyDown += (_, args) => { if (args.Key == Key.Escape) ClosePicker(); };
        popup.Closed += (_, _) =>
        {
            // HEX uses LostFocus by default; commit the pending text even when mouse capture closes the Popup first.
            if (Keyboard.FocusedElement is TextBox text && picker.IsAncestorOf(text))
                text.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            SetCurrentValue(SelectedBrushProperty, picker.SelectedBrush.CloneCurrentValue());
            if (owner != null) owner.Deactivated -= deactivate;
            _pickerPopup = null;
            popup.Child = null;
            // Dispose only after detaching: the standalone ColorPicker also closes its hosting window.
            picker.Dispose();
        };
        popup.IsOpen = true;
    }
}
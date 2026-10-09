using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

internal static partial class Program
{
    private static void VerifyStaticStyleOverrides(Application app)
    {
        // Static defaults in compiled dictionaries are captured. Callers configure existing
        // properties in a BasedOn style; no live settings registry is needed.
        var defaults = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse("""
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:sys="clr-namespace:System;assembly=mscorlib"
                                xmlns:hc="clr-namespace:HandyControl.Controls;assembly=HandyControl">
                <ResourceDictionary.MergedDictionaries>
                    <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml" />
                </ResourceDictionary.MergedDictionaries>
                <sys:Double x:Key="MarkerSize">22</sys:Double>
                <sys:Double x:Key="ArrowSize">19</sys:Double>
                <Style x:Key="CallerCheck" TargetType="CheckBox" BasedOn="{StaticResource CheckBoxBaseStyle}">
                    <Setter Property="Padding" Value="13,0,0,0" />
                    <Setter Property="hc:IconElement.Width" Value="{StaticResource MarkerSize}" />
                    <Setter Property="hc:IconElement.Height" Value="{StaticResource MarkerSize}" />
                </Style>
                <Style x:Key="CallerExpander" TargetType="Expander" BasedOn="{StaticResource ExpanderBaseStyle}">
                    <Setter Property="Padding" Value="17,0,0,0" />
                    <Setter Property="hc:IconElement.Width" Value="{StaticResource ArrowSize}" />
                    <Setter Property="hc:TitleElement.TitleWidth" Value="41" />
                </Style>
            </ResourceDictionary>
            """);
        var checkStyle = (Style)defaults["CallerCheck"];
        var expanderStyle = (Style)defaults["CallerExpander"];
        var check = new CheckBox { Content = "Static", Style = checkStyle };
        var expander = new Expander { Header = "Static", Style = expanderStyle };
        var panel = new StackPanel();
        panel.Children.Add(check); panel.Children.Add(expander);
        panel.Measure(new Size(400, 300)); panel.Arrange(new Rect(0, 0, 400, 300)); panel.UpdateLayout();
        Require(check.Padding == new Thickness(13, 0, 0, 0), "Caller style must configure static padding.");
        Require(VisualChildren<Border>(check).Any(b => b.Width == 22), "Static marker size must use the configured value.");
        var arrow = VisualChildren<Path>(expander).Single(p => p.Name == "PathArrow");
        Require(arrow.Width == 19, "Static arrow size override must reach the actual template.");
        Require(VisualChildren<Grid>(expander).Any(g => g.ColumnDefinitions.Any(c => c.Width == new GridLength(41))),
            "Static arrow column resource must retain GridLength type.");
        defaults["MarkerSize"] = 27d;
        defaults["ArrowSize"] = 25d;
        panel.UpdateLayout();
        Require(check.Padding.Left == 13 && arrow.Width == 19, "Existing static consumers must not track replacement.");
        Console.WriteLine("PASS caller styles configure static marker/arrow size, header padding and column width; captured values remain stable.");
    }

    private static IEnumerable<T> VisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed) yield return typed;
            foreach (var nested in VisualChildren<T>(child)) yield return nested;
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using HandyControl.Interactivity;
using Hc = HandyControl.Controls;

internal static partial class Program
{
    private static void VerifyPropertyTitleWidth(Application app)
    {
        var grid = new Hc.PropertyGrid { Width = 500, SelectedObject = new TitleWidthModel() };
        var window = new Window
        {
            Content = grid, Width = 800, Height = 600, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false
        };
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            window.UpdateLayout();
        }
        void Check(string scenario)
        {
            Flush();
            var expected = Math.Max(grid.MinTitleWidth, Math.Min(grid.MaxTitleWidth, grid.ActualWidth / 3));
            var items = (ItemsControl)grid.Template.FindName("PART_ItemsControl", grid);
            foreach (var item in CollectionViewSource.GetDefaultView(items.ItemsSource).Cast<Hc.PropertyItem>())
            {
                var header = (TextBlock)FindDescendant<GroupBox>(item)!.Header;
                Require(Math.Abs(header.ActualWidth - expected) < 1,
                    $"{scenario}: {item.PropertyName} title width={header.ActualWidth:F2}, expected={expected:F2}; inherited arrow width must not replace the property column.");
                var headerBounds = header.TransformToAncestor(item).TransformBounds(new Rect(header.RenderSize));
                var editorBounds = item.EditorElement.TransformToAncestor(item).TransformBounds(new Rect(item.EditorElement.RenderSize));
                Require(editorBounds.Left >= headerBounds.Right,
                    $"{scenario}: {item.PropertyName} editor overlaps title: titleRight={headerBounds.Right:F2}, editorLeft={editorBounds.Left:F2}.");
                var text = new FormattedText(header.Text, System.Globalization.CultureInfo.CurrentCulture,
                    header.FlowDirection, new Typeface(header.FontFamily, header.FontStyle, header.FontWeight, header.FontStretch),
                    header.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(header).PixelsPerDip);
                Require(header.ActualWidth - header.Padding.Left - header.Padding.Right >= text.WidthIncludingTrailingWhitespace,
                    scenario + ": ordinary property names must fit without ellipsis: " + header.Text);
                if (item.PropertyName == nameof(TitleWidthModel.String))
                    Require(item.EditorElement.ActualWidth > 100,
                        $"{scenario}: string editor must retain usable width, actual={item.EditorElement.ActualWidth:F2}.");
            }
        }
        try
        {
            window.Show();
            Check("grouped 500 DIP");
            Console.WriteLine($"PASS grouped property title width: {Hc.TitleElement.GetTitleWidth(grid).Value:F2} DIP at 500 DIP.");
            var expander = FindDescendant<Expander>(grid)!;
            var arrowWidth = Hc.TitleElement.GetTitleWidth(expander);
            Require(arrowWidth == (GridLength)app.FindResource("ExpanderArrowColumnWidth"), "Expander must retain its independent arrow column width.");
            expander.IsExpanded = false;
            Flush();
            expander.IsExpanded = true;
            Check("re-expanded");
            Hc.TitleElement.SetTitleWidth(expander, new GridLength(48));
            Check("custom arrow width");
            ControlCommands.SortByName.Execute(null, grid);
            Check("sort by name");
            ControlCommands.SortByCategory.Execute(null, grid);
            Check("sort by category");
            foreach (var width in new[] { 360d, 720d, 500d })
            {
                grid.Width = width;
                Check("resized " + width);
            }
            grid.MinTitleWidth = 200;
            grid.MaxTitleWidth = 260;
            Check("limits changed without resize");
            grid.MinTitleWidth = 220;
            grid.MaxTitleWidth = 220;
            Check("fixed width without resize");
            grid.MinTitleWidth = 180;
            grid.MaxTitleWidth = 240;
            grid.Width = 660;
            Check("custom title limits");
            Console.WriteLine("PASS PropertyGrid title widths: grouping, sorting, expand/collapse, custom arrow width, resizing and title limits.");
        }
        finally { window.Close(); }
    }

    private sealed class TitleWidthModel
    {
        [System.ComponentModel.Category("Example")]
        public string String { get; set; } = "Value";
        [System.ComponentModel.Category("Example")]
        public int Integer { get; set; } = 10;
        public bool Boolean { get; set; } = true;
    }
}

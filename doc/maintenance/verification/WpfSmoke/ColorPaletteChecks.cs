using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Hc = HandyControl.Controls;

internal static partial class Program
{
    /// <summary>
    /// 验证 ColorPalette 在窄编辑列内的收缩行为：色板必须随宿主宽度收缩，当前颜色按钮始终完整落在
    /// 色板与编辑列内（不再被最右裁掉），色块可缩但不溢出；含透明度的 #AARRGGBB 全文保留在
    /// HexText、ToolTip 与 Automation 名称里；点击当前颜色按钮打开的取色弹层在关闭时回写颜色
    /// （沿用既有 CurrentColor / Swatches / HexText / _pickerPopup 契约，兼容下游旧测试）。
    /// </summary>
    private static void VerifyColorPalette(Application app)
    {
        foreach (var width in new[] { 160d, 220d, 320d })
        {
            var palette = new Hc.ColorPalette
            {
                SelectedBrush = new SolidColorBrush(Color.FromArgb(0x80, 0x12, 0x34, 0x56))
            };
            var slot = new Border { Width = width, HorizontalAlignment = HorizontalAlignment.Left, Child = palette };
            var window = new Window
            {
                Content = slot, Width = width + 60, Height = 220, Left = -10000, Top = -10000,
                ShowActivated = false, ShowInTaskbar = false
            };
            try
            {
                window.Show();
                app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                window.UpdateLayout();

                var current = (Button)palette.FindName("CurrentColor")
                    ?? throw new InvalidOperationException("ColorPalette lacks CurrentColor.");
                var swatches = (UniformGrid)palette.FindName("Swatches")
                    ?? throw new InvalidOperationException("ColorPalette lacks Swatches.");
                var hex = (TextBlock)palette.FindName("HexText")
                    ?? throw new InvalidOperationException("ColorPalette lacks HexText.");

                var currentBounds = current.TransformToAncestor(palette).TransformBounds(new Rect(current.RenderSize));
                var swatchBounds = swatches.TransformToAncestor(palette).TransformBounds(new Rect(swatches.RenderSize));
                Console.WriteLine($"  palette({width}dp): palette={palette.ActualWidth} swatches={swatchBounds} current={currentBounds} hex={hex.Text} tooltip='{current.ToolTip}' automation='{AutomationProperties.GetName(current)}'");

                // 关键回归：修复前根 UserControl 有 MinWidth=288，窄列下色板被撑到 288 并溢出，
                // 最右的当前颜色按钮被裁掉。修复后必须收敛到宿主宽度。
                Require(Math.Abs(palette.ActualWidth - width) <= 0.5d,
                    $"palette width {width}: the palette must shrink to its slot instead of overflowing: {palette.ActualWidth}");

                Require(currentBounds.Left >= -0.5d && currentBounds.Right <= palette.ActualWidth + 0.5d,
                    $"palette width {width}: the current-color button must stay inside the palette: {currentBounds} of {palette.ActualWidth}");
                Require(currentBounds.Width > 0d && current.ActualHeight > 0d,
                    $"palette width {width}: the current-color button must keep a usable size: {currentBounds}");

                Require(swatchBounds.Left >= -0.5d && swatchBounds.Width > 0d,
                    $"palette width {width}: the swatch grid must stay visible: {swatchBounds}");
                Require(swatchBounds.Right <= currentBounds.Left + 0.5d,
                    $"palette width {width}: the swatch grid must not overlap the current-color button: {swatchBounds.Right} vs {currentBounds.Left}");
                foreach (Button swatch in swatches.Children)
                {
                    Require(swatch.ActualWidth > 0d && swatch.ActualHeight > 0d,
                        $"palette width {width}: a preset color must stay visible after shrinking: {swatch.ActualWidth}x{swatch.ActualHeight}");
                }

                Require(hex.Text == "#80123456",
                    $"palette width {width}: the preview must keep the full #AARRGGBB text: {hex.Text}");
                Require(current.ToolTip is string tooltip && tooltip.Contains("#80123456", StringComparison.Ordinal),
                    $"palette width {width}: the current-color button must expose the full color in its ToolTip: {current.ToolTip}");
                Require(AutomationProperties.GetName(current).Contains("#80123456", StringComparison.Ordinal),
                    $"palette width {width}: the current-color button must expose the full color in its Automation name: {AutomationProperties.GetName(current)}");

                RequireNoLayoutClip(current, $"palette width {width} current-color button");
                RequireNoLayoutClip(swatches, $"palette width {width} swatch grid");

                if (width == 320d)
                {
                    VerifyColorPalettePopupCommit(app, palette);
                }
            }
            finally { window.Close(); }
        }
        Console.WriteLine("PASS color palette (shrinks to 160/220/320 without clipping; full ARGB kept in text/tooltip/automation; popup close commits).");
    }

    /// <summary>
    /// 复现下游旧测试的弹层契约：点击当前颜色按钮打开完整取色器，弹层关闭时把取色结果回写。
    /// </summary>
    private static void VerifyColorPalettePopupCommit(Application app, Hc.ColorPalette palette)
    {
        var current = (Button)palette.FindName("CurrentColor")!;
        var initial = palette.SelectedBrush.Color;
        current.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, current));
        var field = typeof(Hc.ColorPalette).GetField("_pickerPopup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ColorPalette must keep its _pickerPopup field.");
        var popup = (Popup)field.GetValue(palette)!;
        Require(popup.IsOpen, "the current-color button must open the detailed picker popup.");
        var picker = (Hc.ColorPicker)popup.Child;
        Require(picker.SelectedBrush.Color == initial,
            $"the picker must start from the palette color: {picker.SelectedBrush.Color} vs {initial}");
        var expected = Color.FromArgb(0x80, 0x11, 0x22, 0x33);
        picker.SelectedBrush = new SolidColorBrush(expected);
        Require(palette.SelectedBrush.Color == initial,
            "the palette must not commit before the popup closes.");
        popup.IsOpen = false;
        Pump(TimeSpan.FromMilliseconds(60));
        Require(palette.SelectedBrush.Color == expected,
            $"closing the picker popup must write the picked color back: {palette.SelectedBrush.Color}");
        Require(popup.Child is null, "closing the popup must detach the picker child.");
        Console.WriteLine($"  palette popup commit: {initial} -> {palette.SelectedBrush.Color}");
    }

    /// <summary>
    /// 加载编译后的 PropertyGridDemo，断言颜色编辑器的当前颜色按钮完整落在编辑列与 Demo 视口内。
    /// </summary>
    private static void VerifyPropertyGridDemoColorEditor(Application app, Assembly assembly)
    {
        var page = (FrameworkElement)Activator.CreateInstance(
            assembly.GetType("HandyControlDemo.UserControl.PropertyGridDemo", true)!)!;
        var window = new Window
        {
            Content = page, Width = 1000, Height = 800, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false
        };
        try
        {
            window.Show();
            window.UpdateLayout();
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            window.UpdateLayout();

            var grid = FindDescendant<Hc.PropertyGrid>(page)
                ?? throw new InvalidOperationException("PropertyGridDemo lacks a PropertyGrid.");
            var scroll = page.FindName("DemoScrollViewer") as ScrollViewer
                ?? throw new InvalidOperationException("PropertyGridDemo lacks DemoScrollViewer.");
            var items = (ItemsControl)grid.Template.FindName("PART_ItemsControl", grid);
            var colorItems = CollectionViewSource.GetDefaultView(items.ItemsSource).Cast<Hc.PropertyItem>()
                .Where(item => item.EditorElement is Hc.ColorPalette).ToArray();
            Require(colorItems.Length >= 1, "PropertyGridDemo must expose at least one color editor.");

            foreach (var item in colorItems)
            {
                var palette = (Hc.ColorPalette)item.EditorElement;
                var slot = (FrameworkElement)VisualTreeHelper.GetParent(palette);
                var current = (Button)palette.FindName("CurrentColor")
                    ?? throw new InvalidOperationException($"{item.PropertyName}: ColorPalette lacks CurrentColor.");
                var inSlot = current.TransformToAncestor(slot).TransformBounds(new Rect(current.RenderSize));
                var inViewport = current.TransformToAncestor(scroll).TransformBounds(new Rect(current.RenderSize));
                Console.WriteLine($"  demo color editor({item.PropertyName}): palette={palette.ActualWidth} slot={slot.ActualWidth} button={inSlot} viewport={scroll.ViewportWidth}");
                Require(palette.ActualWidth <= slot.ActualWidth + 0.5d,
                    $"{item.PropertyName}: the color palette must fit its editor slot: {palette.ActualWidth} vs {slot.ActualWidth}");
                Require(inSlot.Left >= -0.5d && inSlot.Right <= slot.ActualWidth + 0.5d,
                    $"{item.PropertyName}: the current-color button must stay inside the editor slot: {inSlot} of {slot.ActualWidth}");
                Require(inViewport.Left >= -0.5d && inViewport.Right <= scroll.ViewportWidth + 0.5d,
                    $"{item.PropertyName}: the current-color button must stay inside the demo viewport: {inViewport} of {scroll.ViewportWidth}");
                RequireNoLayoutClip(current, $"{item.PropertyName} current-color button");
            }
            Console.WriteLine("PASS compiled PropertyGridDemo color editors: the current-color button stays inside the editor slot and the viewport.");
        }
        finally { window.Close(); }
    }

    /// <summary>断言元素没有被任一祖先的布局裁剪切掉（真实的“遮挡/裁切”判据）。</summary>
    private static void RequireNoLayoutClip(FrameworkElement element, string label)
    {
        var bounds = new Rect(element.RenderSize);
        for (FrameworkElement? node = element; node is not null; node = VisualTreeHelper.GetParent(node) as FrameworkElement)
        {
            var projected = element.TransformToAncestor(node).TransformBounds(bounds);
            var clip = LayoutInformation.GetLayoutClip(node);
            Require(clip is null || clip.Bounds.Contains(projected),
                $"{label} is clipped by {node.GetType().Name}: {projected} vs {clip?.Bounds}");
            if (node is Window) break;
        }
    }

    /// <summary>跑一段消息循环，让 Popup 关闭等异步收尾完成。</summary>
    private static void Pump(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = duration };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
}

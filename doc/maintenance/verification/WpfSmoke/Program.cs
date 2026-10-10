using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using HandyControl.Data;
using Hc = HandyControl.Controls;

internal static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        try
        {
            VerifyEnumDataProvider();
            if (args.Length == 1 && args[0] == "--enum-only") return 0;
            if (args.Length == 2 && args[0] == "--frame-only")
            {
                VerifyFrameDemo(app, args[1]);
                return 0;
            }
            foreach (var name in new[] { "CategoryOrderingAttribute", "DisplayNameOrderingAttribute",
                         "DateTimePickerAttribute", "DateTimePickType", "DecimalRoundAttribute",
                         "FilePathSelectorAttribute", "FormatAttribute", "OpenDirectoryPropertyAttribute" })
                Require(typeof(Hc.PropertyGrid).Assembly.GetType("HandyControl.Data." + name) is null,
                    "HandyControl must not export business metadata: " + name);
            Console.WriteLine("PASS removed metadata types are absent from HandyControl DLL.");
            foreach (var skin in new[] { "SkinDefault", "SkinDark", "SkinViolet" })
            {
                app.Resources.MergedDictionaries.Clear();
                foreach (var name in new[] { skin, "Theme" })
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/HandyControl;component/Themes/{name}.xaml")
                    });

                VerifyStaticStyleOverrides(app);
                VerifyPropertyTitleWidth(app);
                VerifyPropertyGrid(app);
                VerifyPropertyGroupExpander(app);
                VerifyClockSwitching(app);
                VerifyNumericUpDown(app);
                VerifyWindowAndGrowl(app);
                VerifyLanguages(app);
                VerifyThemeTokenOverride(app);
                VerifyButtonSizing(app);
                VerifyIconClipping(app);
                VerifyFlattenedWrappers(app);
                VerifyNumericUpDownHeight(app);
                VerifyPropertyGridToolbar(app);
                VerifyButtonGroupItems(app);
                VerifyControlTokenSizing(app);
                VerifyCardHeaderSlots(app);
                Console.WriteLine($"PASS {skin}: PropertyGrid ordering/enum editor, ClockType switching, NumericUpDown binding, template replacement, limits, WindowChrome, Growl, runtime design-token override, button icon sizing, numeric spinner height, PropertyGrid toolbar height, button group item sizing, control token sizing, card header slots.");
            }
            if (args.Length == 1)
            {
                VerifyDemoPages(app, args[0]);
                VerifyFrameDemo(app, args[0]);
            }
            Console.WriteLine("PASS all WPF binary smoke checks.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            Hc.Growl.GrowlPanel = null;
            app.Shutdown();
        }
    }

    private static void VerifyNumericUpDown(Application app)
    {
        var control = new Hc.NumericUpDown
        {
            Minimum = -10, Maximum = 10, Value = 3,
            SelectionTextBrush = Brushes.Red
        };
        TextBox? previous = null;
        // Each style has a different template: this exercises OnApplyTemplate repeatedly.
        foreach (var style in new[] { (Style)app.FindResource(typeof(Hc.NumericUpDown)),
                     (Style)app.FindResource("NumericUpDownExtend"), (Style)app.FindResource("NumericUpDownPlus") })
        {
            control.Style = style;
            control.ApplyTemplate();
            control.Measure(new Size(320, 80));
            control.Arrange(new Rect(0, 0, 320, 80));
            var textBox = control.Template.FindName("PART_TextBox", control) as TextBox
                ?? throw new InvalidOperationException("NumericUpDown template lacks PART_TextBox.");
            Require(!ReferenceEquals(previous, textBox), "Template replacement must create a new TextBox.");
            var binding = BindingOperations.GetBindingExpression(textBox, TextBoxBase.SelectionTextBrushProperty)
                ?? throw new InvalidOperationException("SelectionTextBrush binding is absent.");
            binding.UpdateTarget();
            Require(ReferenceEquals(textBox.SelectionTextBrush, Brushes.Red), "Initial brush must reach TextBox.");
            control.SelectionTextBrush = Brushes.Blue;
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            Require(ReferenceEquals(textBox.SelectionTextBrush, Brushes.Blue), "Brush changes must propagate.");
            textBox.Text = "7";
            Require(control.Value == 7, "Text input must update Value.");
            control.Value = 99;
            Require(control.Value == 10, "Maximum must clamp Value.");
            control.Value = -99;
            Require(control.Value == -10, "Minimum must clamp Value.");
            control.SelectionTextBrush = Brushes.Red;
            previous = textBox;
        }
    }

    private static void VerifyWindowAndGrowl(Application app)
    {
        var panel = new StackPanel();
        var window = new Hc.Window
        {
            Content = panel, Width = 480, Height = 320,
            ShowInTaskbar = false, ShowActivated = false,
            Left = -10000, Top = -10000,
            Style = (Style)app.FindResource("WindowWin10")
        };
        try
        {
            window.Show();
            window.UpdateLayout();
            Require(WindowChrome.GetWindowChrome(window) != null, "Modern WindowChrome must be installed.");
            Require(window.Template != null, "Window template must load.");
            Hc.Growl.GrowlPanel = panel;
            Hc.Growl.Info(new GrowlInfo { Message = "WPF smoke", StaysOpen = true });
            window.UpdateLayout();
            Require(panel.Children.Count == 1 && panel.Children[0] is Hc.Growl { Message: "WPF smoke" },
                "Growl must appear in the registered panel.");
            Hc.Growl.Clear();
            Require(panel.Children.Count == 0, "Growl.Clear must empty the panel.");
        }
        finally
        {
            Hc.Growl.GrowlPanel = null;
            window.Close();
        }
    }

    private static void VerifyLanguages(Application app)
    {
        var button = new Button();
        HandyControl.Properties.Langs.LangProvider.SetLang(button, ContentControl.ContentProperty, "Confirm");
        foreach (var (culture, expected) in new[]
                 { ("zh-cn", "确定"), ("en", "Confirm"), ("en-US", "Confirm"), ("zh-cn", "确定") })
        {
            HandyControl.Tools.ConfigHelper.Instance.SetLang(culture);
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            Require(HandyControl.Properties.Langs.Lang.Confirm == expected,
                $"Resource lookup failed for {culture}.");
            Require(Equals(button.Content, expected), $"Live language binding failed for {culture}.");
        }
        var satellite = typeof(Hc.NumericUpDown).Assembly.GetSatelliteAssembly(
            System.Globalization.CultureInfo.GetCultureInfo("en"));
        Require(satellite.GetName().CultureName == "en", "English satellite must be loadable.");
        Console.WriteLine("PASS Chinese/English lookup, en-US fallback, live binding and English satellite loading.");
    }

    private static void VerifyPropertyGrid(Application app)
    {
        var model = new SortModel();
        var grid = new Hc.PropertyGrid
        {
            Style = (Style)app.FindResource(typeof(Hc.PropertyGrid)),
            SelectedObject = model
        };
        grid.ApplyTemplate();
        var items = (ItemsControl)grid.Template.FindName("PART_ItemsControl", grid);
        var view = CollectionViewSource.GetDefaultView(items.ItemsSource);
        string Order() => string.Join(",", view.Cast<Hc.PropertyItem>().Select(x => x.PropertyName));
        Require(Order() == "Other,ReadOnly,Second,First", "Attribute category/property order failed: " + Order());
        var originalItems = view.Cast<Hc.PropertyItem>().ToDictionary(x => x.PropertyName);
        var editors = originalItems.ToDictionary(x => x.Key, x => x.Value.EditorElement);
        var combo = (ComboBox)editors["First"];
        Require(combo.Items.Cast<EnumItem>().Select(x => x.Description).SequenceEqual(new[] { "Ready description", "Running" }),
            "Enum description and field-name fallback must both be present.");
        Require(Equals(combo.SelectedValue, TestState.Ready), "Initial enum selection failed.");
        combo.SelectedValue = TestState.Running;
        Require(model.First == TestState.Running, "Enum editor must write the enum value back to the model.");
        Require(!editors["ReadOnly"].IsEnabled, "Read-only enum editor must be disabled.");

        grid.CategoryOrder = new[] { "B", "A" };
        grid.PropertyOrder = new[] { "First" };
        Require(Order() == "First,Second,Other,ReadOnly", "External priorities must override matching attribute priorities: " + Order());
        foreach (var item in view.Cast<Hc.PropertyItem>())
            Require(ReferenceEquals(originalItems[item.PropertyName], item) && ReferenceEquals(editors[item.PropertyName], item.EditorElement),
                "Sorting must preserve property items and editor instances.");

        HandyControl.Interactivity.ControlCommands.SortByName.Execute(null, grid);
        Require(Order() == "First,Other,ReadOnly,Second" && view.GroupDescriptions.Count == 0, "Alphabetical sort failed.");
        grid.CategoryOrder = new[] { "A", "B" };
        Require(Order() == "First,Other,ReadOnly,Second" && view.GroupDescriptions.Count == 0, "Changing priority must preserve alphabetical mode.");

        grid.CategoryOrder = null;
        grid.PropertyOrder = null;
        HandyControl.Interactivity.ControlCommands.SortByCategory.Execute(null, grid);
        Require(Order() == "Other,ReadOnly,Second,First", "Clearing external priorities must restore attribute order: " + Order());
        foreach (var descriptor in System.ComponentModel.TypeDescriptor.GetProperties(typeof(OrderEdgeModel)).Cast<System.ComponentModel.PropertyDescriptor>())
        {
            var expected = descriptor.Name == nameof(OrderEdgeModel.Padded) ? 10 : int.MaxValue;
            Require(new Hc.PropertyResolver().ResolvePropertyOrder(descriptor) == expected,
                "Invalid/missing order must fall back without throwing: " + descriptor.Name);
        }
        Console.WriteLine("PASS PropertyGrid attribute priorities, external overrides, runtime sorting, editor identity and enum round-trip.");
    }

    private static void VerifyClockSwitching(Application app)
    {
        var selected = new DateTime(2030, 2, 3, 4, 5, 6);
        var calendar = new Hc.CalendarWithClock { SelectedDateTime = selected, ShowConfirmButton = true };
        var picker = new Hc.DateTimePicker { SelectedDateTime = selected, ClockType = ClockType.ListClock };
        var panel = new StackPanel();
        panel.Children.Add(calendar);
        panel.Children.Add(picker);
        var window = new Window { Content = panel, Width = 800, Height = 700, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            window.UpdateLayout();
        }
        Hc.ClockBase Clock(Hc.CalendarWithClock owner) => (Hc.ClockBase)((ContentPresenter)owner.Template.FindName("PART_ClockPresenter", owner)).Content;
        try
        {
            window.Show();
            Flush();
            Require(Clock(calendar) is Hc.Clock, "Default calendar must use Clock.");
            var pending = new DateTime(2030, 2, 4, 17, 28, 39);
            calendar.DisplayDateTime = pending;
            var previous = Clock(calendar);
            calendar.ClockType = ClockType.ListClock;
            Flush();
            var listClock = (Hc.ListClock)Clock(calendar);
            var title = calendar.Template.FindName("ListClockTitle", calendar) as TextBlock;
            Require(title is { Visibility: Visibility.Visible } && title.Text == pending.ToString("HH:mm:ss"),
                "Generated theme must contain the visible ListClock title with the pending time.");
            Require(((ContentPresenter)calendar.Template.FindName("PART_ClockPresenter", calendar)).Margin.Top == 50,
                "Generated theme must apply ListClock layout.");
            Require(calendar.DisplayDateTime == pending && calendar.SelectedDateTime == selected,
                "Clock switch must preserve unconfirmed time and committed selection.");
            var hour = (ListBox)listClock.Template.FindName("PART_HourList", listClock);
            var minute = (ListBox)listClock.Template.FindName("PART_MinuteList", listClock);
            var second = (ListBox)listClock.Template.FindName("PART_SecondList", listClock);
            Require(hour.SelectedIndex == 17 && minute.SelectedIndex == 28 && second.SelectedIndex == 39,
                "ListClock must display all pending time components.");
            AssertListClockRows(hour, "hour");
            AssertListClockRows(minute, "minute");
            AssertListClockRows(second, "second");
            var changes = 0;
            var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(Hc.CalendarWithClock.DisplayDateTimeProperty, typeof(Hc.CalendarWithClock));
            EventHandler changed = (_, _) => changes++;
            descriptor.AddValueChanged(calendar, changed);
            previous.DisplayTime = pending.AddHours(2);
            Require(changes == 0, "Detached clock must not update the calendar.");
            descriptor.RemoveValueChanged(calendar, changed);
            hour.SelectedIndex = 21;
            minute.SelectedIndex = 42;
            second.SelectedIndex = 51;
            pending = new DateTime(2030, 2, 4, 21, 42, 51);
            Require(calendar.DisplayDateTime == pending && calendar.SelectedDateTime == selected, "List selection must remain unconfirmed and retain the calendar date.");
            calendar.ClockType = ClockType.Clock;
            Flush();
            Require(Clock(calendar) is Hc.Clock && calendar.DisplayDateTime == pending, "Switch back must retain edits.");
            calendar.ClockType = ClockType.ListClock;
            Flush();
            var confirmations = 0;
            calendar.Confirmed += () => confirmations++;
            ((Button)calendar.Template.FindName("PART_ButtonConfirm", calendar)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(calendar.SelectedDateTime == pending && confirmations == 1, "Calendar confirmation must commit once.");
            var popup = (Popup)picker.Template.FindName("PART_Popup", picker);
            var inner = (Hc.CalendarWithClock)popup.Child;
            Require(inner.ClockType == ClockType.ListClock, "Initial picker ClockType binding failed.");
            picker.IsDropDownOpen = true;
            Flush();
            Require(Clock(inner) is Hc.ListClock, "Picker popup must load a ListClock.");
            inner.DisplayDateTime = pending;
            picker.ClockType = ClockType.Clock;
            Flush();
            Require(Clock(inner) is Hc.Clock && inner.DisplayDateTime == pending, "Open popup clock switch must retain pending edits.");
            picker.ClockType = ClockType.ListClock;
            Flush();
            ((Button)inner.Template.FindName("PART_ButtonConfirm", inner)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Flush();
            Require(picker.SelectedDateTime == pending && !picker.IsDropDownOpen, "Picker confirmation must commit and close popup.");
            foreach (var styleName in new[] { "DateTimePickerExtend", "DateTimePickerPlus", "DateTimePickerPlus.Small" })
            {
                picker.Style = (Style)app.FindResource(styleName);
                Flush();
                picker.IsDropDownOpen = true;
                Flush();
                popup = (Popup)picker.Template.FindName("PART_Popup", picker);
                inner = (Hc.CalendarWithClock)popup.Child;
                Require(Clock(inner) is Hc.ListClock && picker.SelectedDateTime == pending,
                    "Picker template replacement must preserve clock type and selection: " + styleName);
                picker.IsDropDownOpen = false;
                Flush();
            }
            var calendarTemplate = calendar.Template;
            calendar.Template = null;
            Flush();
            calendar.Template = calendarTemplate;
            Flush();
            Require(Clock(calendar) is Hc.ListClock && calendar.DisplayDateTime == pending,
                "Calendar template replacement must preserve active clock and pending time.");
            ((Button)calendar.Template.FindName("PART_ButtonConfirm", calendar)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Require(confirmations == 2, "Template replacement must not duplicate confirmation handlers.");
            foreach (var control in new DependencyObject[] { calendar, picker })
            {
                var property = control is Hc.DateTimePicker ? Hc.DateTimePicker.ClockTypeProperty : Hc.CalendarWithClock.ClockTypeProperty;
                var rejected = false;
                try { control.SetValue(property, (ClockType)99); }
                catch (ArgumentException) { rejected = true; }
                Require(rejected, "Undefined ClockType must be rejected.");
            }
            Console.WriteLine("PASS Clock/ListClock switching, pending time, detached events, list edits, picker popup and confirmation.");
        }
        finally { picker.IsDropDownOpen = false; window.Close(); }
    }

    /// <summary>
    /// 断言候选列表实际按“已实化的候选项行高 × 8”渲染：可见行数由候选项真实尺寸决定，
    /// 因此不能写死像素高度；同时列表必须能滚动到可见范围之外。
    /// </summary>
    private static void AssertListClockRows(ListBox list, string name)
    {
        var container = list.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem
            ?? throw new InvalidOperationException($"{name} list must realize its first candidate item.");
        var rowHeight = container.ActualHeight + container.Margin.Top + container.Margin.Bottom;
        Require(rowHeight > 0d, $"{name} list candidate row height must be measurable: {rowHeight}");
        var expected = rowHeight * 8d;
        Require(Math.Abs(list.ActualHeight - expected) <= 0.5d,
            $"{name} list must show exactly 8 candidate rows: {list.ActualHeight} vs {expected} (row={rowHeight})");
        var scrollViewer = FindDescendant<ScrollViewer>(list)
            ?? throw new InvalidOperationException($"{name} list must host its candidates in a ScrollViewer.");
        Require(scrollViewer.ScrollableHeight > 0d,
            $"{name} list must keep the remaining candidates scrollable: {scrollViewer.ScrollableHeight}");
        Console.WriteLine($"  {name} list: row={rowHeight} height={list.ActualHeight} expected8rows={expected} scrollable={scrollViewer.ScrollableHeight}");
    }

    public enum TestState
    {
        [System.ComponentModel.Description("Ready description")] Ready,
        Running
    }

    public sealed class SortModel
    {
        [System.ComponentModel.Category("B"), System.ComponentModel.DisplayName("Alpha"),
         SmokeFixtures.CategoryOrderingAttribute("B", 20),
         SmokeFixtures.DisplayNameOrderingAttribute("Alpha", 20)]
        public TestState First { get; set; }
        [System.ComponentModel.Category("B"), System.ComponentModel.DisplayName("Zulu"),
         SmokeFixtures.CategoryOrderingAttribute("B", 20),
         SmokeFixtures.DisplayNameOrderingAttribute("Zulu", 10)]
        public string Second { get; set; } = "value";
        [System.ComponentModel.Category("A"),
         SmokeFixtures.CategoryOrderingAttribute("A", 10)]
        public string Other { get; set; } = "other";
        [System.ComponentModel.Category("A"),
         SmokeFixtures.CategoryOrderingAttribute("A", 10)]
        public TestState ReadOnly => TestState.Ready;
    }

    private sealed class DisplayNameOrderingAttribute(object order) : Attribute
    {
        public object Order { get; } = order;
    }

    private sealed class OrderEdgeModel
    {
        [DisplayNameOrdering("00000010")]
        public string Padded { get; set; } = "";
        [DisplayNameOrdering("invalid")]
        public string Invalid { get; set; } = "";
        [DisplayNameOrdering(9223372036854775807L)]
        public string Overflow { get; set; } = "";
        public string Plain { get; set; } = "";
    }

    private static void VerifyDemoPages(Application app, string demoPath)
    {
        var assembly = System.Reflection.Assembly.LoadFrom(System.IO.Path.GetFullPath(demoPath));
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/HandyControlDemo;component/Resources/Themes/SkinDefault.xaml")
        });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/HandyControlDemo;component/Resources/Themes/Theme.xaml")
        });
        foreach (var name in new[] { "PropertyGridDemo", "CalendarWithClockDemo", "DateTimePickerDemo" })
        {
            var page = (FrameworkElement)Activator.CreateInstance(assembly.GetType("HandyControlDemo.UserControl." + name, true)!)!;
            var window = new Window { Content = page, Width = 1000, Height = 800, Left = -10000, Top = -10000,
                ShowActivated = false, ShowInTaskbar = false };
            try
            {
                window.Show();
                window.UpdateLayout();
                app.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
                if (name != "PropertyGridDemo")
                {
                    var toggle = (CheckBox)page.FindName("UseListClock");
                    var target = (DependencyObject)page.FindName(name == "DateTimePickerDemo" ? "SwitchablePicker" : "SwitchableCalendar");
                    var property = target is Hc.DateTimePicker ? Hc.DateTimePicker.ClockTypeProperty : Hc.CalendarWithClock.ClockTypeProperty;
                    Require(Equals(target.GetValue(property), ClockType.ListClock), name + " initial trigger failed.");
                    toggle.IsChecked = false;
                    app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
                    Require(Equals(target.GetValue(property), ClockType.Clock), name + " toggle binding failed.");
                }
                Console.WriteLine("PASS compiled Demo page load and bindings: " + name);
            }
            finally { window.Close(); }
        }
        VerifyPropertyGridDemoScrolling(app, assembly);
        VerifyCardDemoPage(app, assembly);
    }

    /// <summary>
    /// 加载编译后的 CardDemo：确认标题插槽按钮带 ToolTip 与 Automation 名称、标题在带插槽时仍居中，
    /// 且点击事件确实走到页面的点击处理器（反馈文本随之变化）。
    /// CardDemo 依赖 Demo 的 Locator 资源，冒烟不启动 Demo App，这里显式补上该资源。
    /// </summary>
    private static void VerifyCardDemoPage(Application app, System.Reflection.Assembly assembly)
    {
        if (app.Resources["Locator"] is null)
        {
            app.Resources["Locator"] = Activator.CreateInstance(
                assembly.GetType("HandyControlDemo.ViewModel.ViewModelLocator", true)!);
        }
        var page = (FrameworkElement)Activator.CreateInstance(
            assembly.GetType("HandyControlDemo.UserControl.CardDemo", true)!)!;
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

            static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
            {
                for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                {
                    var child = VisualTreeHelper.GetChild(root, i);
                    if (child is T typed) yield return typed;
                    foreach (var nested in Descendants<T>(child)) yield return nested;
                }
            }

            var slotCard = Descendants<Hc.Card>(page).FirstOrDefault(card =>
                Hc.EdgeElement.GetLeftContent(card) is Button && Hc.EdgeElement.GetRightContent(card) is not null);
            Require(slotCard is not null, "CardDemo must show a card with both header slots.");
            var slotButton = (Button)Hc.EdgeElement.GetLeftContent(slotCard!);
            var toolTip = slotButton.ToolTip as string;
            Require(!string.IsNullOrEmpty(toolTip)
                    && !string.IsNullOrEmpty(System.Windows.Automation.AutomationProperties.GetName(slotButton)),
                "CardDemo header slot buttons must expose ToolTip and AutomationProperties.Name.");
            var header = slotCard!.Template.FindName("PART_Header", slotCard) as Border
                ?? throw new InvalidOperationException("CardDemo card lacks PART_Header.");
            var presenter = slotCard.Template.FindName("PART_HeaderContent", slotCard) as ContentPresenter
                ?? throw new InvalidOperationException("CardDemo card lacks PART_HeaderContent.");
            var title = FindDescendant<TextBlock>(presenter)
                ?? throw new InvalidOperationException("CardDemo header lacks a title TextBlock.");
            var titleBounds = title.TransformToAncestor(header).TransformBounds(new Rect(title.RenderSize));
            var titleCenter = titleBounds.X + titleBounds.Width / 2d;
            var expectedCenter = (header.Padding.Left + header.ActualWidth - header.Padding.Right) / 2d;
            Require(Math.Abs(titleCenter - expectedCenter) <= 1d,
                $"CardDemo title must stay centered with header slots: {titleCenter} vs {expectedCenter}");

            var feedback = page.FindName("HeaderSlotActionText") as TextBlock
                ?? throw new InvalidOperationException("CardDemo lacks the HeaderSlotActionText feedback block.");
            var before = feedback.Text;
            slotButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, slotButton));
            var after = feedback.Text;
            Require(after != before && after!.Contains(toolTip, StringComparison.Ordinal),
                $"CardDemo header slot click must reach the page handler: '{before}' -> '{after}'");
            Console.WriteLine($"PASS compiled CardDemo page: header slots, tooltip/automation, centered title, click handler -> {after}");
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// PropertyGridDemo 在受限窗口下必须能垂直滚动到底：内容高于视口时滚动范围必须大于 0，
    /// 滚动到底后页面底部进入视口，且页面顶部移出视口。
    /// </summary>
    private static void VerifyPropertyGridDemoScrolling(Application app, System.Reflection.Assembly assembly)
    {
        var page = (FrameworkElement)Activator.CreateInstance(
            assembly.GetType("HandyControlDemo.UserControl.PropertyGridDemo", true)!)!;
        var window = new Window
        {
            Content = page, Width = 420, Height = 320, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false
        };
        try
        {
            window.Show();
            var scroll = page.FindName("DemoScrollViewer") as ScrollViewer
                ?? throw new InvalidOperationException("PropertyGridDemo must host its content in a ScrollViewer.");
            window.UpdateLayout();
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
            window.UpdateLayout();
            Require(scroll.ViewportHeight > 0d && scroll.ScrollableHeight > 0d,
                $"PropertyGridDemo content must exceed the constrained viewport: viewport={scroll.ViewportHeight} scrollable={scroll.ScrollableHeight}");
            var content = (FrameworkElement)scroll.Content;
            var viewport = new Rect(0, 0, scroll.ViewportWidth, scroll.ViewportHeight);
            Rect Bounds() => content.TransformToAncestor(scroll).TransformBounds(new Rect(content.RenderSize));
            Require(Math.Abs(scroll.VerticalOffset) <= 0.5d,
                "PropertyGridDemo must start at the top: " + scroll.VerticalOffset);
            Require(Bounds().Bottom > viewport.Bottom + 1d,
                $"PropertyGridDemo must overflow the viewport: content bottom {Bounds().Bottom} vs viewport {viewport.Bottom}");
            scroll.ScrollToEnd();
            window.UpdateLayout();
            Require(Math.Abs(scroll.VerticalOffset - scroll.ScrollableHeight) <= 0.5d,
                $"PropertyGridDemo must scroll to the bottom: {scroll.VerticalOffset} of {scroll.ScrollableHeight}");
            Require(Bounds().Bottom <= viewport.Bottom + 2d && Bounds().Top < 0d,
                $"Reaching the end must reveal the page bottom and hide the top: {Bounds()} in {viewport}");
            Console.WriteLine($"PASS PropertyGridDemo scrolls to the bottom inside a constrained window: viewport={viewport.Height} content={Bounds().Height}");
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// 验证设计令牌的动态消费：字体家族/字号/最小高度/内外边距必须能被运行时覆盖，
    /// 并且覆盖 HandyControl 的 Color 资源后已解析的画刷要随动、PrimaryBrush 类型不变。
    /// </summary>
    private static void VerifyThemeTokenOverride(Application app)
    {
        var button = new Button { Style = (Style)app.FindResource(typeof(Button)) };
        var textBox = new TextBox { Style = (Style)app.FindResource(typeof(TextBox)) };
        var panel = new StackPanel();
        panel.Children.Add(button);
        panel.Children.Add(textBox);
        // 控件必须位于活动可视树中，DynamicResource 的失效通知才会沿树传播。
        var window = new Window
        {
            Content = panel, Width = 400, Height = 200,
            Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false
        };
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();
        }
        try
        {
            window.Show();
            Flush();

            Require(button.FontSize == 12d, "BaseStyle must consume TextFontSize: " + button.FontSize);
            Require(button.FontFamily.Source.Contains("Microsoft YaHei UI", StringComparison.OrdinalIgnoreCase),
                "BaseStyle must consume DefaultFontFamily: " + button.FontFamily.Source);
            Require(double.IsNaN(button.Height), "ButtonBaseBaseStyle must leave Height at Auto: " + button.Height);
            Require(button.MinHeight == 28d, "ButtonBaseBaseStyle must consume ButtonMinHeight: " + button.MinHeight);
            Require(button.Padding == new Thickness(10, 5, 10, 5),
                "ButtonBaseBaseStyle must consume ButtonPadding: " + button.Padding);
            Require(textBox.MinHeight == 28d, "InputElementBaseStyle must consume ButtonMinHeight: " + textBox.MinHeight);
            Require(textBox.Padding == new Thickness(8, 0, 8, 0),
                "InputElementBaseStyle must consume InputPadding: " + textBox.Padding);

            var baselineTextColor = ((SolidColorBrush)app.FindResource("PrimaryTextBrush")).Color;
            Require(app.FindResource("PrimaryBrush") is LinearGradientBrush,
                "PrimaryBrush must stay a gradient brush for animation compatibility.");

            // 运行时主题覆盖：字号/字体/尺寸类令牌由样式 Setter 的 DynamicResource 跟随；
            // HandyControl 画刷自身的 DynamicResource 颜色只在首次解析时求值，
            // 因此颜色必须由框架直接提供同键的实体画刷（PrimaryBrush 保持渐变类型）。
            var primaryGradient = new LinearGradientBrush(Colors.Lime, Colors.Green, new Point(0, 0), new Point(1, 0));
            var overrides = new ResourceDictionary
            {
                ["TextFontSize"] = 20d,
                ["DefaultFontFamily"] = new FontFamily("Consolas"),
                ["ButtonMinHeight"] = 44d,
                ["InputMinHeight"] = 44d,
                ["ButtonPadding"] = new Thickness(20, 5, 20, 5),
                ["InputPadding"] = new Thickness(16, 0, 16, 0),
                ["PrimaryTextColor"] = Colors.Lime,
                ["PrimaryTextBrush"] = new SolidColorBrush(Colors.Lime),
                ["ButtonForegroundBrush"] = new SolidColorBrush(Colors.Lime),
                ["InputBackgroundBrush"] = new SolidColorBrush(Colors.Teal),
                ["PrimaryBrush"] = primaryGradient
            };
            app.Resources.MergedDictionaries.Add(overrides);
            Flush();

            Require(button.FontSize == 20d, "TextFontSize override must reach a live control: " + button.FontSize);
            Require(button.FontFamily.Source.Contains("Consolas", StringComparison.OrdinalIgnoreCase),
                "DefaultFontFamily override must reach a live control: " + button.FontFamily.Source);
            Require(button.MinHeight == 44d, "ButtonMinHeight override must reach a live button: " + button.MinHeight);
            Require(button.Padding == new Thickness(20, 5, 20, 5),
                "ButtonPadding override must reach a live button: " + button.Padding);
            Require(textBox.MinHeight == 44d, "ButtonMinHeight override must reach a live TextBox: " + textBox.MinHeight);
            Require(textBox.Padding == new Thickness(16, 0, 16, 0),
                "InputPadding override must reach a live TextBox: " + textBox.Padding);
            Require(ReferenceEquals(app.FindResource("PrimaryTextBrush"), overrides["PrimaryTextBrush"]),
                "A real brush resource must override HandyControl's frozen-at-first-use brush.");
            Require(ReferenceEquals(app.FindResource("PrimaryBrush"), primaryGradient)
                    && app.FindResource("PrimaryBrush") is LinearGradientBrush,
                "The accent gradient key must stay a LinearGradientBrush.");
            Require(button.Foreground is SolidColorBrush foreground && foreground.Color == Colors.Lime,
                "Live control foreground must follow the theme brush override.");
            Require(textBox.Background is SolidColorBrush background && background.Color == Colors.Teal,
                "Live input background must follow the theme brush override.");

            app.Resources.MergedDictionaries.Remove(overrides);
            Flush();

            Require(button.FontSize == 12d && button.MinHeight == 28d,
                "Removing the override must restore baseline tokens.");
            Require(((SolidColorBrush)app.FindResource("PrimaryTextBrush")).Color == baselineTextColor,
                "Removing the override must restore the baseline theme color.");
            Console.WriteLine("PASS runtime design-token override (font family/size, control height, padding, theme brushes).");
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// 验证按钮图标尺寸：Height=Auto 之后，模板中 Stretch=Uniform 的图标 Path 不能被大坐标几何撑开，
    /// 纯图标按钮必须有显式尺寸（圆形保持 1:1），Small 按钮的显式 Height 不能被继承的 MinHeight 压过，
    /// 并且 ButtonMinHeight 的运行时覆盖要同时改变图标按钮尺寸而不影响 Small。
    /// </summary>
    private static void VerifyPropertyGroupExpander(Application app)
    {
        var grid = new Hc.PropertyGrid { FontSize = 19, SelectedObject = new { Name = "标题字号", Count = 3 } };
        Hc.TitleElement.SetFontSize(grid, 15);
        var body = new TextBlock { Text = "正文" };
        var native = new Expander { Header = "原生标题", FontSize = 19, Content = body, IsExpanded = true };
        var panel = new StackPanel();
        panel.Children.Add(grid);
        panel.Children.Add(native);
        var window = new Window
        {
            Content = panel, Width = 560, Height = 640,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false
        };
        try
        {
            window.Show();
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();
            var group = FindDescendant<GroupItem>(grid)!;
            var expander = FindDescendant<Expander>(group)!;
            var toggle = (ToggleButton)expander.Template.FindName("ToggleButton", expander);
            var header = FindDescendant<TextBlock>(toggle)!;
            Require(ReferenceEquals(expander.Template, native.Template), "Property groups must use the native expander template.");
            Require(Equals(expander.Background, native.Background) && expander.BorderThickness == native.BorderThickness
                && Hc.BorderElement.GetCornerRadius(expander) == Hc.BorderElement.GetCornerRadius(native),
                "Property groups must preserve native expander background, border and corners.");
            Require(header.FontSize == 15 && expander.FontSize == 19, "PropertyGrid title/body font sizes must be independent.");
            var contentBorder = (Border)expander.Content;
            var headerBorder = (Border)toggle.Template.FindName("Chrome", toggle);
            Require(contentBorder.BorderThickness == new Thickness(1, 0, 1, 1)
                && Equals(contentBorder.BorderBrush, expander.BorderBrush)
                && Equals(contentBorder.Background, app.FindResource("RegionBrush")),
                "Group content must match the native Expander demo: no second border at the header seam.");
            // Distinct edge widths catch accidental reuse of the global Region border or a doubled seam.
            foreach (var thickness in new[] { new Thickness(2, 3, 4, 5), new Thickness(0), new Thickness(1) })
            {
                expander.BorderThickness = thickness;
                expander.BorderBrush = Brushes.Crimson;
                Hc.BorderElement.SetCornerRadius(expander, new CornerRadius(3, 5, 7, 9));
                window.UpdateLayout();
                Require(headerBorder.BorderThickness == thickness
                    && contentBorder.BorderThickness == new Thickness(thickness.Left, 0, thickness.Right, thickness.Bottom),
                    "One Expander.BorderThickness must drive both regions without doubling the shared seam.");
                Require(Equals(headerBorder.BorderBrush, Brushes.Crimson)
                    && Equals(contentBorder.BorderBrush, Brushes.Crimson), "One border brush must drive both regions.");
                Require(Hc.BorderElement.GetCornerRadius(toggle) == new CornerRadius(3, 5, 0, 0)
                    && contentBorder.CornerRadius == new CornerRadius(0, 0, 7, 9), "Corners must share the expander source.");
                Require(Math.Abs(contentBorder.TransformToAncestor(expander).Transform(new Point()).Y
                    - (toggle.TransformToAncestor(expander).Transform(new Point()).Y + toggle.ActualHeight)) < .01,
                    "Header and content must meet without a gap or overlap.");
            }
            expander.ClearValue(Control.BorderThicknessProperty);
            expander.ClearValue(Control.BorderBrushProperty);
            expander.ClearValue(Hc.BorderElement.CornerRadiusProperty);
            window.UpdateLayout();
            Hc.TitleElement.SetFontSize(grid, 23);
            window.UpdateLayout();
            Require(header.FontSize == 23 && expander.FontSize == 19, "Changing group title font must leave body font unchanged.");
            var arrow = FindDescendant<Path>(toggle)!;
            toggle.IsChecked = false;
            window.UpdateLayout();
            Require(!expander.IsExpanded && ((ContentPresenter)expander.Template.FindName("ExpandSite", expander)).Visibility == Visibility.Collapsed,
                "Native toggle must collapse the group content.");
            Require(Math.Abs(arrow.TransformToAncestor(toggle).Transform(new Point()).Y + arrow.ActualHeight / 2 - toggle.ActualHeight / 2) <= 1,
                "Collapsed arrow must remain vertically centered.");
            toggle.IsChecked = true;
            window.UpdateLayout();
            Require(expander.IsExpanded, "Native toggle must reopen the group.");
            foreach (var direction in new[] { ExpandDirection.Down, ExpandDirection.Up, ExpandDirection.Left, ExpandDirection.Right })
            {
                native.ExpandDirection = direction;
                Hc.TitleElement.SetFontSize(native, 12);
                window.UpdateLayout();
                var nativeToggle = (ToggleButton)native.Template.FindName("ToggleButton", native);
                var nativeHeader = FindDescendant<TextBlock>(nativeToggle)!;
                Require(nativeHeader.FontSize == 12 && body.FontSize == 19, $"{direction}: independent default title/body size.");
                Hc.TitleElement.SetFontSize(native, 27);
                window.UpdateLayout();
                Require(nativeHeader.FontSize == 27 && body.FontSize == 19, $"{direction}: runtime title size must not alter content.");
                native.FontSize = 21;
                window.UpdateLayout();
                Require(nativeHeader.FontSize == 27 && body.FontSize == 21, $"{direction}: body size must not alter title.");
                native.FontSize = 19;
            }
            Console.WriteLine("PASS native PropertyGrid appearance/toggle and Expander title/body fonts in all four directions.");
        }
        finally { window.Close(); }
    }
    private static void VerifyButtonSizing(Application app)
    {
        var hugeIcon = (Geometry)app.FindResource("DeleteGeometry");
        var wideIcon = (Geometry)app.FindResource("DownGeometry");
        var textButton = CreateIconButton((Style)app.FindResource(typeof(Button)), hugeIcon, "Text");
        var wideTextButton = CreateIconButton((Style)app.FindResource(typeof(Button)), wideIcon, "Text");
        var iconButton = CreateIconButton((Style)app.FindResource("ButtonIcon"), hugeIcon);
        var circularButton = CreateIconButton((Style)app.FindResource("ButtonIconCircular"), hugeIcon);
        var circularSmall = CreateIconButton((Style)app.FindResource("ButtonIconCircular.Small"), hugeIcon);
        var smallTextButton = new Button
        {
            Style = (Style)app.FindResource("ButtonDefault.Small"), Content = "Small", Margin = new Thickness(4)
        };

        var panel = new StackPanel();
        foreach (var child in new UIElement[] { textButton, wideTextButton, iconButton, circularButton, circularSmall, smallTextButton })
            panel.Children.Add(child);
        var window = new Window
        {
            Content = panel, Width = 480, Height = 480,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false
        };
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();
        }

        try
        {
            window.Show();
            Flush();

            // 修复前：Height=Auto 让图标 Path 按几何原始坐标（Delete/Down 都是 1024 级）测量，
            // 普通按钮会被撑到上千像素；图标框也必须回到 DefaultIconSize。
            foreach (var (name, element) in new[] { ("icon+text", textButton), ("wide icon+text", wideTextButton) })
            {
                Require(element.ActualHeight <= 40d,
                    $"{name} button must keep the control height instead of the geometry: {element.ActualHeight}");
                Require(element.ActualWidth <= 120d,
                    $"{name} button width must stay bounded by the icon box: {element.ActualWidth}");
                var icon = FindDescendant<Path>(element)
                    ?? throw new InvalidOperationException($"{name} button template lacks an icon Path.");
                Require(icon.ActualWidth <= 20d && icon.ActualHeight <= 20d,
                    $"{name} icon box must stay at DefaultIconSize: {icon.ActualWidth}x{icon.ActualHeight}");
            }

            Require(iconButton.ActualWidth == 28d && iconButton.ActualHeight == 28d,
                $"ButtonIcon must carry an explicit square size: {iconButton.ActualWidth}x{iconButton.ActualHeight}");
            Require(circularButton.ActualWidth == 28d && circularButton.ActualHeight == 28d,
                $"ButtonIconCircular must stay 1:1 at ButtonMinHeight: {circularButton.ActualWidth}x{circularButton.ActualHeight}");
            Require(circularSmall.ActualWidth == 20d && circularSmall.ActualHeight == 20d,
                $"ButtonIconCircular.Small must stay 20x20: {circularSmall.ActualWidth}x{circularSmall.ActualHeight}");
            Require(smallTextButton.ActualHeight == 20d,
                $"ButtonDefault.Small explicit Height must not be overridden by MinHeight: {smallTextButton.ActualHeight}");

            var overrides = new ResourceDictionary { ["ButtonMinHeight"] = 44d };
            app.Resources.MergedDictionaries.Add(overrides);
            Flush();
            Require(textButton.ActualHeight == 44d,
                $"ButtonMinHeight override must grow a text button: {textButton.ActualHeight}");
            Require(iconButton.ActualWidth == 44d && iconButton.ActualHeight == 44d,
                $"ButtonMinHeight override must resize ButtonIcon: {iconButton.ActualWidth}x{iconButton.ActualHeight}");
            Require(circularButton.ActualWidth == 44d && circularButton.ActualHeight == 44d,
                $"ButtonIconCircular must stay 1:1 under a runtime size override: {circularButton.ActualWidth}x{circularButton.ActualHeight}");
            Require(smallTextButton.ActualHeight == 20d && circularSmall.ActualHeight == 20d,
                "Small buttons must keep their explicit height under a runtime size override.");

            app.Resources.MergedDictionaries.Remove(overrides);
            Flush();
            Require(iconButton.ActualWidth == 28d && circularButton.ActualHeight == 28d,
                "Removing the override must restore the baseline button size.");
            Console.WriteLine("PASS button sizing (icon geometry containment, icon-only square size, circular 1:1, Small MinHeight, runtime size override).");
        }
        finally { window.Close(); }
    }

    private static void VerifyIconClipping(Application app)
    {
        var keys = new List<string>
        {
            "ButtonIcon", "ButtonIconCircular", "RepeatButtonIcon", "RepeatButtonIconCircular", "RadioButtonIcon",
            "ToggleButtonIcon", "ToggleButtonIconPrimary", "ToggleButtonIconSuccess", "ToggleButtonIconInfo",
            "ToggleButtonIconWarning", "ToggleButtonIconDanger", "ToggleButtonIconTransparent"
        };
        keys.AddRange(keys.ToArray().Select(key => key + ".Small"));
        var panel = new WrapPanel();
        var buttons = new List<(string Key, ButtonBase Button)>();
        foreach (var key in keys)
        {
            var style = (Style)app.FindResource(key);
            foreach (var enabled in new[] { true, false })
            {
                var button = (ButtonBase)Activator.CreateInstance(style.TargetType)!;
                button.Style = style;
                button.IsEnabled = enabled;
                button.Margin = new Thickness(5);
                Hc.IconElement.SetGeometry(button, (Geometry)app.FindResource("UpDownGeometry"));
                Hc.IconSwitchElement.SetGeometry(button, (Geometry)app.FindResource("UpDownGeometry"));
                Hc.IconSwitchElement.SetGeometrySelected(button, (Geometry)app.FindResource("DeleteGeometry"));
                panel.Children.Add(button);
                buttons.Add((key, button));
            }
        }
        var window = new Window { Content = panel, Width = 1000, Height = 600,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            foreach (var selected in new[] { false, true })
            {
                foreach (var (_, button) in buttons)
                    if (button is ToggleButton toggle) toggle.IsChecked = selected;
                window.UpdateLayout();
                foreach (var (key, button) in buttons)
                {
                    var icon = FindDescendant<Path>(button)
                        ?? throw new InvalidOperationException(key + " lacks an icon Path.");
                    Require(icon.ActualWidth > 0 && icon.ActualHeight > 0, key + " has an empty icon.");
                    // 检查图标及其祖先的真实布局裁剪，按钮外框尺寸正常并不代表图标完整。
                    var bounds = new Rect(icon.RenderSize);
                    for (Visual? node = icon; node is not null; node = VisualTreeHelper.GetParent(node) as Visual)
                    {
                        var projected = icon.TransformToAncestor(node).TransformBounds(bounds);
                        var clip = VisualTreeHelper.GetClip(node);
                        Require(clip is null || clip.Bounds.Contains(projected),
                            $"{key} enabled={button.IsEnabled} selected={selected}: {node.GetType().Name} clips icon {projected} to {clip?.Bounds}.");
                        if (ReferenceEquals(node, button)) break;
                    }
                }
            }
            Console.WriteLine("PASS 24 icon styles, enabled/disabled and checked states: no icon layout clipping.");
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// 验证模板冗余单子包装已扁平化：图标模板中包裹单个 Path 的 ContentControl 已移除，Path 直接挂在
    /// 模板 Border 下并保留控件内边距；ImageViewer 的无装饰透传 Border 已移除，尺寸/对齐/边距仍由
    /// PART_ImageMain 承担。图标尺寸与裁剪另由 VerifyIconClipping / VerifyButtonSizing 覆盖。
    /// </summary>
    private static void VerifyFlattenedWrappers(Application app)
    {
        var geometry = (Geometry)app.FindResource("UpDownGeometry");
        var icons = new (string Key, Control Control)[]
        {
            ("ButtonIcon", new Button()),
            ("RepeatButtonIcon", new RepeatButton()),
            ("ToggleButtonIcon", new ToggleButton()),
            ("ToggleButtonIconTransparent", new ToggleButton()),
            ("ToggleBlockIcon", new Hc.ToggleBlock())
        };

        var panel = new WrapPanel();
        foreach (var (key, control) in icons)
        {
            control.Style = (Style)app.FindResource(key);
            control.Margin = new Thickness(5);
            Hc.IconElement.SetGeometry(control, geometry);
            Hc.IconElement.SetWidth(control, 16);
            Hc.IconElement.SetHeight(control, 16);
            Hc.IconSwitchElement.SetGeometry(control, geometry);
            panel.Children.Add(control);
        }

        var viewer = new Hc.ImageViewer();
        panel.Children.Add(viewer);

        var window = new Window
        {
            Content = panel, Width = 900, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false
        };
        try
        {
            window.Show();
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();

            foreach (var (key, control) in icons)
            {
                var path = FindDescendant<Path>(control)
                    ?? throw new InvalidOperationException(key + " lacks an icon Path.");
                Require(VisualTreeHelper.GetParent(path) is not ContentControl,
                    key + " still wraps its icon Path in a redundant ContentControl.");
                Require(path.ActualWidth > 0 && path.ActualHeight > 0, key + " has an empty icon.");
                Require(path.Margin == control.Padding,
                    $"{key} icon Margin must follow the control Padding: {path.Margin} vs {control.Padding}.");
            }

            var image = FindDescendant<Image>(viewer)
                ?? throw new InvalidOperationException("ImageViewer lacks PART_ImageMain.");
            Require(VisualTreeHelper.GetParent(image) is not Border,
                "ImageViewer still wraps PART_ImageMain in a redundant pass-through Border.");
            Require(image.Margin == viewer.ImageMargin
                && image.HorizontalAlignment == HorizontalAlignment.Left
                && image.VerticalAlignment == VerticalAlignment.Top,
                $"ImageViewer must keep its margin/alignment on the image itself: {image.Margin}.");

            Console.WriteLine("PASS flattened single-child wrappers (icon content hosts + ImageViewer pass-through border).");
        }
        finally { window.Close(); }
    }

    private static Button CreateIconButton(Style style, Geometry geometry, string? content = null)
    {
        var button = new Button { Style = style, Margin = new Thickness(4) };
        if (content is not null) button.Content = content;
        Hc.IconElement.SetGeometry(button, geometry);
        return button;
    }

    /// <summary>
    /// 验证 NumericUpDown 的内嵌上下按钮是半高子按钮：它们必须各自 MinHeight=0，否则两行都会吃到
    /// 按钮基类继承来的 MinHeight=ButtonMinHeight，把整个数字框撑成双倍高度。
    /// 默认字号下三种模板的数字框都要与同字体、同内边距的 TextBox/ComboBox 同高；
    /// 动态放大 TextFontSize 时数字框随内容增高，而不是被固定高度裁切。
    /// </summary>
    private static void VerifyNumericUpDownHeight(Application app)
    {
        var textBox = new TextBox();
        var comboBox = new ComboBox { ItemsSource = new[] { "one", "two" }, SelectedIndex = 0 };
        var numbers = new[]
        {
            new Hc.NumericUpDown { Value = 3 },
            new Hc.NumericUpDown { Value = 3, Style = (Style)app.FindResource("NumericUpDownExtend") },
            new Hc.NumericUpDown { Value = 3, Style = (Style)app.FindResource("NumericUpDownPlus") }
        };
        var panel = new StackPanel();
        panel.Children.Add(textBox);
        panel.Children.Add(comboBox);
        foreach (var number in numbers) panel.Children.Add(number);
        var window = new Window
        {
            Content = panel, Width = 420, Height = 360,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false
        };
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();
        }
        void RequireConsistent(string stage)
        {
            Require(Math.Abs(numbers[0].ActualHeight - textBox.ActualHeight) <= 1d,
                $"{stage}: NumericUpDown must match the TextBox height: {numbers[0].ActualHeight} vs {textBox.ActualHeight}");
            Require(Math.Abs(numbers[0].ActualHeight - comboBox.ActualHeight) <= 2d,
                $"{stage}: NumericUpDown must match the ComboBox height: {numbers[0].ActualHeight} vs {comboBox.ActualHeight}");
            foreach (var number in numbers)
            {
                var up = number.Template?.FindName("UpButton", number) as RepeatButton
                    ?? throw new InvalidOperationException($"{stage}: NumericUpDown template lacks UpButton.");
                var down = number.Template?.FindName("DownButton", number) as RepeatButton
                    ?? throw new InvalidOperationException($"{stage}: NumericUpDown template lacks DownButton.");
                Require(up.MinHeight == 0d && down.MinHeight == 0d,
                    $"{stage}: the inner spinner buttons must reset MinHeight: {up.MinHeight}/{down.MinHeight}");
                Require(number.ActualHeight <= textBox.ActualHeight * 1.5d,
                    $"{stage}: the numeric box must not double its height: {number.ActualHeight} vs {textBox.ActualHeight}");
                Require(up.ActualHeight <= number.ActualHeight / 2d + 1d,
                    $"{stage}: the spinner button must stay a half-height child: {up.ActualHeight} of {number.ActualHeight}");
            }
        }

        try
        {
            var hostFontSize = window.FontSize;
            window.Show();
            Flush();
            RequireConsistent("default font");
            Console.WriteLine($"  input heights (default): text={textBox.ActualHeight} combo={comboBox.ActualHeight} numeric={numbers[0].ActualHeight}/{numbers[1].ActualHeight}/{numbers[2].ActualHeight}");

            // 输入控件按继承取字体（输入样式不自己吃 TextFontSize），所以用宿主窗口字号模拟
            // Ultron 在窗口层驱动的动态字号：四种输入必须同步增高，数字框不得翻倍或被裁切。
            window.FontSize = 24d;
            Flush();
            Require(numbers[0].FontSize == 24d && textBox.FontSize == 24d && comboBox.FontSize == 24d,
                "Every input must inherit the host font size.");
            RequireConsistent("font 24");
            Require(numbers[0].ActualHeight > 28d,
                "The numeric box must grow with the font instead of clipping it: " + numbers[0].ActualHeight);
            Console.WriteLine($"  input heights (font 24): text={textBox.ActualHeight} combo={comboBox.ActualHeight} numeric={numbers[0].ActualHeight}/{numbers[1].ActualHeight}/{numbers[2].ActualHeight}");

            window.FontSize = hostFontSize;
            Flush();
            RequireConsistent("restored font");
            Console.WriteLine("PASS NumericUpDown height (spinner MinHeight=0, TextBox/ComboBox parity, growth at font 24).");
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// 验证 PropertyGrid 原生工具栏的排序按钮与搜索框实际同高：两者都必须消费动态
    /// ButtonMinHeight（单选组项此前写死 StaticResource 的 Height，搜索框内容行写死
    /// StaticResource 的 MinContentHeight）。覆盖默认、Ultron 规格 36、48、字号 24 以及密度
    /// 0.75/1.5 的组合，并且字号高于令牌时必须由内容把输入框撑高，而不是被固定高度裁切。
    /// </summary>
    private static void VerifyPropertyGridToolbar(Application app)
    {
        var propertyGrid = new Hc.PropertyGrid
        {
            Style = (Style)app.FindResource(typeof(Hc.PropertyGrid)),
            SelectedObject = new SortModel(),
            Width = 360
        };
        var window = new Window
        {
            Content = propertyGrid, Width = 420, Height = 260,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false
        };
        var hostFontSize = window.FontSize;
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();
        }

        try
        {
            window.Show();
            Flush();
            var searchBar = propertyGrid.Template?.FindName("PART_SearchBar", propertyGrid) as Hc.SearchBar
                ?? throw new InvalidOperationException("PropertyGrid template lacks PART_SearchBar.");
            var group = FindDescendant<Hc.ButtonGroup>(propertyGrid)
                ?? throw new InvalidOperationException("PropertyGrid template lacks the sort ButtonGroup.");
            var sortButtons = group.Items.OfType<RadioButton>().ToArray();
            Require(sortButtons.Length == 2, "The sort ButtonGroup must hold the two sort buttons: " + sortButtons.Length);

            var scenarios = new[]
            {
                (28d, 12d), (36d, 12d), (36d, 24d), (48d, 12d), (48d, 24d),
                (21d, 9d), (42d, 18d), (27d, 12d), (54d, 12d), (28d, 24d)
            };
            foreach (var (height, font) in scenarios)
            {
                var overrides = new ResourceDictionary { ["InputMinHeight"] = height, ["ButtonMinHeight"] = height };
                window.FontSize = font;
                app.Resources.MergedDictionaries.Add(overrides);
                Flush();
                var label = $"ButtonMinHeight={height} font={font}";
                Console.WriteLine($"  toolbar({label}): search={searchBar.ActualHeight} (min={searchBar.MinHeight} minContent={Hc.InfoElement.GetMinContentHeight(searchBar)} desired={searchBar.DesiredSize.Height}) group={group.ActualHeight} (align={group.VerticalAlignment} desired={group.DesiredSize.Height}) sort={string.Join("/", sortButtons.Select(button => $"{button.ActualHeight}[min={button.MinHeight},align={button.VerticalAlignment}]"))}");
                Require(searchBar.ActualHeight >= height - 0.5d,
                    $"{label}: the search bar must not fall below the shared token: {searchBar.ActualHeight}");
                if (font * 1.4d > height + 0.5d)
                {
                    Require(searchBar.ActualHeight > height + 0.5d,
                        $"{label}: content taller than the token must grow the input instead of being clipped: {searchBar.ActualHeight}");
                }
                foreach (var sortButton in sortButtons)
                {
                    Require(Math.Abs(sortButton.ActualHeight - searchBar.ActualHeight) <= 0.5d,
                        $"{label}: sort button {sortButton.ActualHeight} must match the search bar {searchBar.ActualHeight}");
                }
                app.Resources.MergedDictionaries.Remove(overrides);
            }

            window.FontSize = hostFontSize;
            Flush();
            foreach (var sortButton in sortButtons)
            {
                Require(Math.Abs(searchBar.ActualHeight - 28d) <= 0.5d && Math.Abs(sortButton.ActualHeight - 28d) <= 0.5d,
                    $"Restoring the baseline must return both to ButtonMinHeight: search={searchBar.ActualHeight} sort={sortButton.ActualHeight}");
            }
            Console.WriteLine("PASS PropertyGrid toolbar height (sort buttons and search bar share the dynamic control height).");
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// 验证按钮组三类项样式（RadioButton / Button / ToggleButton）统一消费动态 ButtonMinHeight：
    /// 不写死高度、随运行时覆盖同高变化。
    /// </summary>
    private static void VerifyButtonGroupItems(Application app)
    {
        var group = new Hc.ButtonGroup();
        var radio = new RadioButton { Content = "R" };
        var button = new Button { Content = "B" };
        var toggle = new ToggleButton { Content = "T" };
        foreach (var item in new ButtonBase[] { radio, button, toggle }) group.Items.Add(item);
        var panel = new StackPanel();
        panel.Children.Add(group);
        var window = new Window
        {
            Content = panel, Width = 320, Height = 160,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false
        };
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();
        }

        try
        {
            window.Show();
            Flush();
            foreach (var height in new[] { 28d, 36d, 48d })
            {
                var overrides = new ResourceDictionary { ["ButtonMinHeight"] = height };
                app.Resources.MergedDictionaries.Add(overrides);
                Flush();
                foreach (var item in new ButtonBase[] { radio, button, toggle })
                {
                    Require(Math.Abs(item.ActualHeight - height) <= 0.5d,
                        $"ButtonGroup item {item.GetType().Name} must consume ButtonMinHeight {height}: {item.ActualHeight}");
                }
                Console.WriteLine($"  button group items(ButtonMinHeight={height}): {radio.ActualHeight}/{button.ActualHeight}/{toggle.ActualHeight}");
                app.Resources.MergedDictionaries.Remove(overrides);
            }
            Flush();
            Console.WriteLine("PASS button group item sizing (RadioButton/Button/ToggleButton share the dynamic control height).");
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// 验证非按钮/输入控件的高度也消费动态设计令牌：Label/Tag 与 GroupBox 标题跟随
    /// DefaultControlHeight，SplitButton 与 Pagination 页码按钮跟随 ButtonMinHeight；
    /// 字号高于令牌时由内容撑高、不被固定高度裁切；SplitButton 的 1024 级图标几何仍受有限
    /// 方框约束；Label/SplitButton 的 Small 变体在令牌放大时保持 20。
    /// </summary>
    private static void VerifyControlTokenSizing(Application app)
    {
        var label = new Label { Style = (Style)app.FindResource(typeof(Label)), Content = "L" };
        var labelSmall = new Label { Style = (Style)app.FindResource("LabelDefault.Small"), Content = "l" };
        var tag = new Hc.Tag { Style = (Style)app.FindResource(typeof(Hc.Tag)), Content = "T" };
        var pagination = new Hc.Pagination { Style = (Style)app.FindResource(typeof(Hc.Pagination)), MaxPageCount = 5 };
        var splitButton = new Hc.SplitButton { Style = (Style)app.FindResource(typeof(Hc.SplitButton)), Content = "S" };
        var splitSmall = new Hc.SplitButton { Style = (Style)app.FindResource("SplitButtonDefault.Small"), Content = "s" };
        var splitIconButton = new Hc.SplitButton { Style = (Style)app.FindResource(typeof(Hc.SplitButton)), Content = "S" };
        Hc.IconElement.SetGeometry(splitIconButton, (Geometry)app.FindResource("DeleteGeometry"));
        var groupBox = new GroupBox
        {
            Style = (Style)app.FindResource(typeof(GroupBox)), Header = "G", Content = new TextBlock { Text = "body" }
        };
        // 同规格参照按钮：SplitButton 属于按钮族，默认、运行时令牌覆盖与更大字号下都应与普通按钮一致。
        var referenceButton = new Button { Style = (Style)app.FindResource(typeof(Button)), Content = "B" };
        var referenceIconButton = new Button { Style = (Style)app.FindResource(typeof(Button)), Content = "B" };
        Hc.IconElement.SetGeometry(referenceIconButton, (Geometry)app.FindResource("DeleteGeometry"));

        var panel = new StackPanel();
        foreach (var child in new FrameworkElement[]
                 {
                     label, labelSmall, tag, pagination, splitButton, splitSmall, splitIconButton, groupBox,
                     referenceButton, referenceIconButton
                 })
            panel.Children.Add(child);
        var window = new Window
        {
            Content = panel, Width = 640, Height = 520,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false
        };
        var hostFontSize = window.FontSize;
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();
        }
        void RequireHeight(string name, FrameworkElement element, double expected)
        {
            Require(Math.Abs(element.ActualHeight - expected) <= 0.5d,
                $"{name} must consume the dynamic control height {expected}: {element.ActualHeight}");
        }

        try
        {
            window.Show();
            Flush();

            var pageButton = pagination.Template?.FindName("PART_ButtonFirst", pagination) as RadioButton
                ?? throw new InvalidOperationException("Pagination template lacks PART_ButtonFirst.");
            var headerBorder = FindDescendant<Border>(groupBox)
                ?? throw new InvalidOperationException("GroupBox template lacks its header Border.");
            var splitIcon = FindDescendant<Path>(splitIconButton)
                ?? throw new InvalidOperationException("SplitButton template lacks its icon Path.");
            var referenceIcon = FindDescendant<Path>(referenceIconButton)
                ?? throw new InvalidOperationException("reference button template lacks its icon Path.");

            Console.WriteLine($"  control heights (default): label={label.ActualHeight} tag={tag.ActualHeight} split={splitButton.ActualHeight} button={referenceButton.ActualHeight} page={pageButton.ActualHeight} labelSmall={labelSmall.ActualHeight} splitSmall={splitSmall.ActualHeight} groupHeader={headerBorder.MinHeight} iconSplit={splitIconButton.ActualHeight} iconButton={referenceIconButton.ActualHeight}");

            RequireHeight("Label", label, 28d);
            RequireHeight("Tag", tag, 28d);
            RequireHeight("Pagination page button", pageButton, 28d);
            Require(Math.Abs(headerBorder.MinHeight - 28d) <= 0.5d,
                $"GroupBox header must consume the dynamic control height: {headerBorder.MinHeight}");
            RequireHeight("LabelDefault.Small", labelSmall, 20d);
            RequireHeight("SplitButtonDefault.Small", splitSmall, 20d);
            // SplitButton 属于按钮族：默认高度必须与普通按钮一致，而不是被写死的 28 固定住。
            Require(Math.Abs(splitButton.ActualHeight - referenceButton.ActualHeight) <= 0.5d,
                $"SplitButton must match a sibling button: {splitButton.ActualHeight} vs {referenceButton.ActualHeight}");
            // 带 1024 级图标时不能按几何原始坐标撑开，且要与同规格的普通图标按钮同高、图标框一致。
            Require(Math.Abs(splitIconButton.ActualHeight - referenceIconButton.ActualHeight) <= 0.5d
                    && splitIconButton.ActualHeight <= 40d,
                $"an icon SplitButton must match an icon button: {splitIconButton.ActualHeight} vs {referenceIconButton.ActualHeight}");
            Require(splitIcon.ActualWidth <= 20d && splitIcon.ActualHeight <= 20d
                    && Math.Abs(splitIcon.ActualHeight - referenceIcon.ActualHeight) <= 0.5d,
                $"the SplitButton icon must stay inside DefaultIconSize: {splitIcon.ActualWidth}x{splitIcon.ActualHeight}");

            var overrides = new ResourceDictionary { ["ButtonMinHeight"] = 44d, ["DefaultControlHeight"] = 44d };
            app.Resources.MergedDictionaries.Add(overrides);
            Flush();
            RequireHeight("Label", label, 44d);
            RequireHeight("Tag", tag, 44d);
            RequireHeight("SplitButton", splitButton, 44d);
            RequireHeight("SplitButton (icon)", splitIconButton, 44d);
            RequireHeight("Pagination page button", pageButton, 44d);
            Require(Math.Abs(headerBorder.MinHeight - 44d) <= 0.5d,
                $"GroupBox header must follow a runtime control-height override: {headerBorder.MinHeight}");
            RequireHeight("LabelDefault.Small", labelSmall, 20d);
            RequireHeight("SplitButtonDefault.Small", splitSmall, 20d);
            Require(Math.Abs(splitButton.ActualHeight - referenceButton.ActualHeight) <= 0.5d
                    && Math.Abs(splitIconButton.ActualHeight - referenceIconButton.ActualHeight) <= 0.5d,
                $"SplitButton must keep button parity under a token override: {splitButton.ActualHeight}/{splitIconButton.ActualHeight} vs {referenceButton.ActualHeight}/{referenceIconButton.ActualHeight}");

            app.Resources.MergedDictionaries.Remove(overrides);
            Flush();
            RequireHeight("Label", label, 28d);
            RequireHeight("SplitButtonDefault.Small", splitSmall, 20d);

            // 按钮族与 Tag 的字号来自 TextFontSize 令牌，Label 等按继承取宿主字号；宿主通常两者一起放大。
            // 这里同时放大令牌与宿主字号，验证非固定高度的控件随内容增高，而不是被写入的死高度裁切。
            var fontOverrides = new ResourceDictionary { ["TextFontSize"] = 24d };
            window.FontSize = 24d;
            app.Resources.MergedDictionaries.Add(fontOverrides);
            Flush();
            Require(label.ActualHeight > 28.5d,
                "the Label must grow with a larger font instead of being clipped: " + label.ActualHeight);
            Require(tag.ActualHeight > 28.5d,
                "the Tag must grow with a larger font instead of being clipped: " + tag.ActualHeight);
            Require(Math.Abs(splitButton.ActualHeight - referenceButton.ActualHeight) <= 0.5d
                    && splitButton.ActualHeight > 28.5d,
                $"the SplitButton must follow the button at a larger font: {splitButton.ActualHeight} vs {referenceButton.ActualHeight}");
            Console.WriteLine($"  control heights (font 24): label={label.ActualHeight} tag={tag.ActualHeight} split={splitButton.ActualHeight} button={referenceButton.ActualHeight}");

            app.Resources.MergedDictionaries.Remove(fontOverrides);
            window.FontSize = hostFontSize;
            Flush();
            RequireHeight("Label", label, 28d);
            RequireHeight("Tag", tag, 28d);
            RequireHeight("SplitButton", splitButton, 28d);
            Console.WriteLine("PASS control token sizing (Label/Tag/SplitButton/Pagination/GroupBox follow the dynamic control height, Small stays 20).");
        }
        finally { window.Close(); }
    }

    /// <summary>
    /// 验证 Card：页脚不再画上分隔线；头部左右标题插槽复用既有 hc:EdgeElement.LeftContent /
    /// RightContent，为空时不占位；有插槽时标题仍相对整个 header 居中（两侧不等宽不偏移、不重叠），
    /// 插槽内按钮可命中并触发 Click；同时覆盖 HeaderTemplate 旧契约、Header 决定整行可见性的约定，
    /// 以及左对齐标题（SimpleCard 约定）不被插槽布局带偏。命名结构为
    /// PART_Header / PART_HeaderContent / PART_HeaderLeftContent / PART_HeaderRightContent。
    /// </summary>
    private static void VerifyCardHeaderSlots(Application app)
    {
        var cardStyle = (Style)app.FindResource(typeof(Hc.Card));

        Button IconButton(string tag)
        {
            var button = new Button { Content = tag, Width = 28d, Height = 28d, Tag = tag, ToolTip = tag };
            System.Windows.Automation.AutomationProperties.SetName(button, tag);
            return button;
        }

        Hc.Card MakeCard(object? header) => new()
        {
            Style = cardStyle,
            Header = header,
            Width = 300d,
            HorizontalAlignment = HorizontalAlignment.Left,
            Content = new TextBlock { Text = "body" }
        };

        var plainCard = MakeCard("标题");
        var asymmetricCard = MakeCard("标题");
        Hc.EdgeElement.SetLeftContent(asymmetricCard, IconButton("left"));
        var rightPanel = new StackPanel { Orientation = Orientation.Horizontal };
        rightPanel.Children.Add(IconButton("right1"));
        rightPanel.Children.Add(IconButton("right2"));
        Hc.EdgeElement.SetRightContent(asymmetricCard, rightPanel);
        var symmetricCard = MakeCard("标题");
        Hc.EdgeElement.SetLeftContent(symmetricCard, IconButton("left"));
        Hc.EdgeElement.SetRightContent(symmetricCard, IconButton("right"));
        var leftOnlyCard = MakeCard("标题");
        var leftOnlyButton = IconButton("left");
        Hc.EdgeElement.SetLeftContent(leftOnlyCard, leftOnlyButton);
        var nullHeaderCard = MakeCard(null);
        Hc.EdgeElement.SetLeftContent(nullHeaderCard, IconButton("left"));
        var emptyHeaderCard = MakeCard(string.Empty);
        Hc.EdgeElement.SetLeftContent(emptyHeaderCard, IconButton("left"));
        Hc.EdgeElement.SetRightContent(emptyHeaderCard, IconButton("right"));
        var leftAlignedCard = MakeCard("标题");
        Hc.TitleElement.SetHorizontalAlignment(leftAlignedCard, HorizontalAlignment.Left);
        var templatedCard = MakeCard("标题");
        templatedCard.HeaderTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse(
            "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><TextBlock Text=\"{Binding}\" FontWeight=\"Bold\"/></DataTemplate>");
        var footerCard = MakeCard("标题");
        footerCard.Footer = "页脚";

        // 极窄（160）+ 不等宽插槽：标题必须被省略/裁剪在中央列内，且不与插槽重叠
        var narrowCard = MakeCard("很长的卡片标题文本用于触发字符省略显示效果");
        narrowCard.Width = 160d;
        Hc.EdgeElement.SetLeftContent(narrowCard, IconButton("left"));
        var narrowRightPanel = new StackPanel { Orientation = Orientation.Horizontal };
        narrowRightPanel.Children.Add(IconButton("right1"));
        narrowRightPanel.Children.Add(IconButton("right2"));
        Hc.EdgeElement.SetRightContent(narrowCard, narrowRightPanel);
        // 两张插槽宽度不同的卡片：共享尺寸必须逐卡隔离（A 需 56，B 只需 28）
        var shareCardA = MakeCard("标题");
        Hc.EdgeElement.SetLeftContent(shareCardA, IconButton("left"));
        var shareCardARight = new StackPanel { Orientation = Orientation.Horizontal };
        shareCardARight.Children.Add(IconButton("right1"));
        shareCardARight.Children.Add(IconButton("right2"));
        Hc.EdgeElement.SetRightContent(shareCardA, shareCardARight);
        var shareCardB = MakeCard("标题");
        Hc.EdgeElement.SetLeftContent(shareCardB, IconButton("left"));
        Hc.EdgeElement.SetRightContent(shareCardB, IconButton("right"));

        var panel = new StackPanel();
        foreach (var card in new[]
                 {
                     plainCard, asymmetricCard, symmetricCard, leftOnlyCard, nullHeaderCard, emptyHeaderCard,
                     leftAlignedCard, templatedCard, footerCard, narrowCard, shareCardA, shareCardB
                 })
            panel.Children.Add(card);
        var window = new Window
        {
            Content = panel, Width = 420, Height = 900,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false
        };
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.DataBind);
            window.UpdateLayout();
        }

        Border HeaderOf(Hc.Card card) => card.Template?.FindName("PART_Header", card) as Border
            ?? throw new InvalidOperationException("Card template lacks PART_Header.");
        ContentPresenter PresenterOf(Hc.Card card) => card.Template?.FindName("PART_HeaderContent", card) as ContentPresenter
            ?? throw new InvalidOperationException("Card template lacks PART_HeaderContent.");
        ContentControl LeftHostOf(Hc.Card card) => card.Template?.FindName("PART_HeaderLeftContent", card) as ContentControl
            ?? throw new InvalidOperationException("Card template lacks PART_HeaderLeftContent.");
        ContentControl RightHostOf(Hc.Card card) => card.Template?.FindName("PART_HeaderRightContent", card) as ContentControl
            ?? throw new InvalidOperationException("Card template lacks PART_HeaderRightContent.");
        Grid HeaderGridOf(Hc.Card card) => HeaderOf(card).Child as Grid
            ?? throw new InvalidOperationException("PART_Header must host the header grid.");
        ColumnDefinition SideColumnOf(Hc.Card card, int index) => HeaderGridOf(card).ColumnDefinitions[index];
        Border MiddlePanelOf(Hc.Card card) => HeaderGridOf(card).Children.OfType<Border>().First(panel => Grid.GetColumn(panel) == 1);
        static Rect BoundsOf(FrameworkElement element, FrameworkElement ancestor) =>
            element.TransformToAncestor(ancestor).TransformBounds(new Rect(element.RenderSize));
        static bool IsWithin(DependencyObject? node, DependencyObject ancestor)
        {
            for (var current = node; current is not null; current = VisualTreeHelper.GetParent(current))
                if (ReferenceEquals(current, ancestor)) return true;
            return false;
        }

        TextBlock TitleOf(Hc.Card card) => FindDescendant<TextBlock>(PresenterOf(card))
            ?? throw new InvalidOperationException("Card header presenter lacks a title TextBlock.");

        Rect RequireTitleCentered(string name, Hc.Card card)
        {
            var header = HeaderOf(card);
            var bounds = BoundsOf(TitleOf(card), header);
            var center = bounds.X + bounds.Width / 2d;
            var expected = (header.Padding.Left + header.ActualWidth - header.Padding.Right) / 2d;
            Require(Math.Abs(center - expected) <= 0.5d,
                $"{name}: the card title must stay centered in the whole header: {center} vs {expected}");
            return bounds;
        }

        void RequireNoOverlap(string name, Hc.Card card)
        {
            var header = HeaderOf(card);
            // 极窄时标题的实际可见区域被中央列裁剪，按可见区域比较才是真实渲染结果。
            var title = Rect.Intersect(BoundsOf(TitleOf(card), header), BoundsOf(MiddlePanelOf(card), header));
            var leftHost = LeftHostOf(card);
            var rightHost = RightHostOf(card);
            if (leftHost.Visibility == Visibility.Visible)
            {
                var left = BoundsOf(leftHost, header);
                Require(left.Right <= title.Left + 0.5d,
                    $"{name}: the left header slot must not overlap the visible title: {left.Right} vs {title.Left}");
            }
            if (rightHost.Visibility == Visibility.Visible)
            {
                var right = BoundsOf(rightHost, header);
                Require(right.Left >= title.Right - 0.5d,
                    $"{name}: the right header slot must not overlap the visible title: {right.Left} vs {title.Right}");
            }
        }

        try
        {
            window.Show();
            Flush();

            Require(HeaderOf(plainCard).Child is Grid,
                "PART_Header must host the slot grid and the title must move to PART_HeaderContent.");
            Require(PresenterOf(plainCard).ContentSource == "Header",
                "PART_HeaderContent must keep consuming Header.");

            // 无插槽：两侧宿主折叠且两侧列零宽 —— 不占位
            Require(LeftHostOf(plainCard).Visibility == Visibility.Collapsed
                    && LeftHostOf(plainCard).ActualWidth <= 0.5d
                    && RightHostOf(plainCard).Visibility == Visibility.Collapsed
                    && RightHostOf(plainCard).ActualWidth <= 0.5d
                    && SideColumnOf(plainCard, 0).ActualWidth <= 0.5d
                    && SideColumnOf(plainCard, 2).ActualWidth <= 0.5d,
                "a card without header slots must not reserve any space beside the title.");
            RequireTitleCentered("no slots", plainCard);

            // 不等宽插槽：两侧列共享 CardHeaderSide 恒等宽 -> 标题整体居中且不重叠
            Require(LeftHostOf(asymmetricCard).Visibility == Visibility.Visible
                    && RightHostOf(asymmetricCard).Visibility == Visibility.Visible,
                "header slots must be visible when their content is set.");
            var asymmetricSide = SideColumnOf(asymmetricCard, 0).ActualWidth;
            Require(Math.Abs(asymmetricSide - SideColumnOf(asymmetricCard, 2).ActualWidth) <= 0.5d
                    && asymmetricSide >= 55.5d,
                $"header side columns must share CardHeaderSide: {asymmetricSide} vs {SideColumnOf(asymmetricCard, 2).ActualWidth}");
            var asymmetricTitle = RequireTitleCentered("asymmetric slots", asymmetricCard);
            RequireNoOverlap("asymmetric slots", asymmetricCard);
            var symmetricTitle = RequireTitleCentered("symmetric slots", symmetricCard);
            Require(Math.Abs((asymmetricTitle.X + asymmetricTitle.Width / 2d) - (symmetricTitle.X + symmetricTitle.Width / 2d)) <= 0.5d,
                $"the card title center must not depend on the slot widths: {asymmetricTitle.X} vs {symmetricTitle.X}");

            // 单侧插槽：另一侧宿主仍折叠零宽，但其列镜像等宽以保证标题整体居中
            Require(RightHostOf(leftOnlyCard).Visibility == Visibility.Collapsed
                    && RightHostOf(leftOnlyCard).ActualWidth <= 0.5d,
                "an empty right header slot host must stay collapsed.");
            Require(Math.Abs(SideColumnOf(leftOnlyCard, 2).ActualWidth - SideColumnOf(leftOnlyCard, 0).ActualWidth) <= 0.5d,
                $"the opposite header column must mirror the filled side: {SideColumnOf(leftOnlyCard, 2).ActualWidth} vs {SideColumnOf(leftOnlyCard, 0).ActualWidth}");
            RequireTitleCentered("left only", leftOnlyCard);
            RequireNoOverlap("left only", leftOnlyCard);

            // 逐卡隔离：不同卡片各自维护自己的共享尺寸（A 需 56、B 只需 28）
            Require(Math.Abs(SideColumnOf(shareCardA, 0).ActualWidth - 56d) <= 0.5d
                    && Math.Abs(SideColumnOf(shareCardB, 0).ActualWidth - 28d) <= 0.5d,
                $"shared column size must stay scoped per card: {SideColumnOf(shareCardA, 0).ActualWidth} vs {SideColumnOf(shareCardB, 0).ActualWidth}");
            RequireTitleCentered("shared scope A", shareCardA);
            RequireTitleCentered("shared scope B", shareCardB);

            // 极窄 160 + 不等宽插槽：标题省略并裁在中央列内，仍整体居中且不与插槽重叠
            var narrowHeader = HeaderOf(narrowCard);
            var narrowTitle = TitleOf(narrowCard);
            var narrowTitleBounds = BoundsOf(narrowTitle, narrowHeader);
            var narrowMiddlePanel = MiddlePanelOf(narrowCard);
            var narrowMiddle = narrowMiddlePanel.ActualWidth;
            var narrowCenter = narrowTitleBounds.X + narrowTitleBounds.Width / 2d;
            var narrowExpected = (narrowHeader.Padding.Left + narrowHeader.ActualWidth - narrowHeader.Padding.Right) / 2d;
            Console.WriteLine($"  narrow card (width 160): header={narrowHeader.ActualWidth} pad={narrowHeader.Padding} side={SideColumnOf(narrowCard, 0).ActualWidth}/{SideColumnOf(narrowCard, 2).ActualWidth} middle={narrowMiddle} titleBounds={narrowTitleBounds} titleWidth={narrowTitle.ActualWidth} trim={narrowTitle.TextTrimming} visible={Rect.Intersect(narrowTitleBounds, BoundsOf(narrowMiddlePanel, narrowHeader))}");
            Require(narrowMiddlePanel.ClipToBounds,
                "the middle header panel must clip a title that is wider than its column.");
            Require(narrowTitle.TextTrimming == TextTrimming.CharacterEllipsis,
                "a long header title must use character ellipsis inside the header.");
            // 可见区域必须落在中央列内且相对整个 header 居中（插槽不与可见标题重叠）
            var narrowVisible = Rect.Intersect(narrowTitleBounds, BoundsOf(narrowMiddlePanel, narrowHeader));
            var narrowVisibleCenter = narrowVisible.X + narrowVisible.Width / 2d;
            Require(narrowMiddle > 0d && narrowVisible.Width <= narrowMiddle + 0.5d,
                $"a narrow title must stay inside the middle column: {narrowVisible.Width} vs {narrowMiddle}");
            Require(Math.Abs(narrowVisibleCenter - narrowExpected) <= 0.5d,
                $"narrow 160: the visible title must stay centered in the whole header: {narrowVisibleCenter} vs {narrowExpected}");
            // 省略号自身有最小宽度，允许极小溢出，但必须被中央列裁掉（只裁剪、不重叠）
            Require(narrowTitleBounds.Width <= narrowMiddle + 8d,
                $"a narrow title may only overflow the middle column by the ellipsis minimum: {narrowTitleBounds.Width} vs {narrowMiddle}");
            RequireNoOverlap("narrow 160", narrowCard);
            var narrowMiddleBounds = BoundsOf(narrowMiddlePanel, narrowHeader);
            Require(narrowMiddleBounds.X >= narrowHeader.Padding.Left + SideColumnOf(narrowCard, 0).ActualWidth - 0.5d
                    && narrowMiddleBounds.Right <= narrowHeader.ActualWidth - narrowHeader.Padding.Right - SideColumnOf(narrowCard, 2).ActualWidth + 0.5d,
                $"the middle panel must sit between the two shared side columns: {narrowMiddleBounds}");

            // 插槽内按钮：保留 ToolTip/Automation 名称、可命中、可触发 Click
            Require((string?)leftOnlyButton.ToolTip == "left" && leftOnlyButton.ActualWidth > 0d,
                "header slot content must keep its ToolTip and layout.");
            var clicked = 0;
            leftOnlyButton.Click += (_, _) => clicked++;
            leftOnlyButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, leftOnlyButton));
            Require(clicked == 1, "header slot buttons must raise Click.");
            var buttonCenter = leftOnlyButton.TransformToAncestor(window)
                .Transform(new Point(leftOnlyButton.ActualWidth / 2d, leftOnlyButton.ActualHeight / 2d));
            var hit = VisualTreeHelper.HitTest(window, buttonCenter)?.VisualHit;
            Require(IsWithin(hit, leftOnlyButton),
                "header slot buttons must be hit-testable (title must not cover them): " + hit?.GetType().Name);

            // Header 为空：整行（含插槽）不显示；Header 为空字符串（非 null）时头部与插槽显示、标题不占位
            Require(HeaderOf(nullHeaderCard).Visibility == Visibility.Collapsed
                    && !LeftHostOf(nullHeaderCard).IsVisible,
                "a null Header must collapse the whole header row including its slots.");
            Require(HeaderOf(emptyHeaderCard).Visibility == Visibility.Visible
                    && LeftHostOf(emptyHeaderCard).Visibility == Visibility.Visible
                    && RightHostOf(emptyHeaderCard).Visibility == Visibility.Visible,
                "an empty (non-null) Header must keep the header row and its slots visible.");
            Require(BoundsOf(PresenterOf(emptyHeaderCard), HeaderOf(emptyHeaderCard)).Width <= 0.5d,
                "an empty Header must not reserve space in the middle of the header.");

            // 旧契约：左对齐标题（SimpleCard）不被插槽布局带偏、HeaderTemplate 仍生效
            var leftAlignedHeader = HeaderOf(leftAlignedCard);
            var leftAlignedBounds = BoundsOf(TitleOf(leftAlignedCard), leftAlignedHeader);
            Require(Math.Abs(leftAlignedBounds.X - leftAlignedHeader.Padding.Left) <= 1d,
                $"a left aligned title must hug the header padding: {leftAlignedBounds.X} vs {leftAlignedHeader.Padding.Left}");
            Require(ReferenceEquals(PresenterOf(templatedCard).ContentTemplate, templatedCard.HeaderTemplate),
                "PART_HeaderContent must keep consuming HeaderTemplate.");
            Require(TitleOf(templatedCard).Text == "标题",
                "HeaderTemplate content must render inside PART_HeaderContent.");

            // 页脚：不再画上分隔线、不消费分隔线画刷，保留外框圆角；头部下分隔线保持
            var footer = footerCard.Template?.FindName("PART_Footer", footerCard) as Border
                ?? throw new InvalidOperationException("Card template lacks PART_Footer.");
            Require(footer.Visibility == Visibility.Visible, "the card footer must be visible when Footer is set.");
            Require(footer.BorderThickness == new Thickness(0),
                "the card footer must not draw a separator line: " + footer.BorderThickness);
            Require(footer.BorderBrush is null, "the card footer must not consume the separator brush.");
            Require(footer.CornerRadius.BottomLeft > 0d && footer.CornerRadius.BottomRight > 0d
                    && footer.CornerRadius.TopLeft <= 0d && footer.CornerRadius.TopRight <= 0d,
                "the card footer must keep the outer corner radius: " + footer.CornerRadius);
            Require(HeaderOf(footerCard).BorderThickness.Bottom > 0d,
                "the card header must keep its bottom separator: " + HeaderOf(footerCard).BorderThickness);

            Console.WriteLine($"  card header slots: plainCenter={RequireTitleCentered("plain", plainCard).X + RequireTitleCentered("plain", plainCard).Width / 2d} asymmetricCenter={asymmetricTitle.X + asymmetricTitle.Width / 2d} symmetricCenter={symmetricTitle.X + symmetricTitle.Width / 2d} footerBorder={footer.BorderThickness} footerRadius={footer.CornerRadius}");
            Console.WriteLine("PASS card header slots (EdgeElement.LeftContent/RightContent, centered title, no overlap, clickable content, footer without separator).");
        }
        finally { window.Close(); }
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed) return typed;
            var found = FindDescendant<T>(child);
            if (found is not null) return found;
        }
        return null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

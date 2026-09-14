using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using HandyControl.Data;
using Hc = HandyControl.Controls;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        try
        {
            foreach (var skin in new[] { "SkinDefault", "SkinDark", "SkinViolet" })
            {
                app.Resources.MergedDictionaries.Clear();
                foreach (var name in new[] { skin, "Theme" })
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/HandyControl;component/Themes/{name}.xaml")
                    });

                VerifyPropertyGrid(app);
                VerifyClockSwitching(app);
                VerifyNumericUpDown(app);
                VerifyWindowAndGrowl(app);
                VerifyLanguages(app);
                Console.WriteLine($"PASS {skin}: PropertyGrid ordering/enum editor, ClockType switching, NumericUpDown binding, template replacement, limits, WindowChrome, Growl.");
            }
            if (args.Length == 1) VerifyDemoPages(app, args[0]);
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
            SelectedObject = model,
            CategoryOrder = new[] { "", "b", "B", "Unknown", "A" },
            PropertyOrder = new[] { "Second", "second", "", "Unknown" }
        };
        grid.ApplyTemplate();
        var items = (ItemsControl)grid.Template.FindName("PART_ItemsControl", grid);
        var view = CollectionViewSource.GetDefaultView(items.ItemsSource);
        string Order() => string.Join(",", view.Cast<Hc.PropertyItem>().Select(x => x.PropertyName));
        Require(Order() == "Second,First,Other,ReadOnly", "Custom category/property order and fallback failed: " + Order());
        var originalItems = view.Cast<Hc.PropertyItem>().ToDictionary(x => x.PropertyName);
        var editors = originalItems.ToDictionary(x => x.Key, x => x.Value.EditorElement);
        var combo = (ComboBox)editors["First"];
        Require(combo.Items.Cast<EnumItem>().Select(x => x.Description).SequenceEqual(new[] { "Ready description", "Running" }),
            "Enum description and field-name fallback must both be present.");
        Require(Equals(combo.SelectedValue, TestState.Ready), "Initial enum selection failed.");
        combo.SelectedValue = TestState.Running;
        Require(model.First == TestState.Running, "Enum editor must write the enum value back to the model.");
        Require(!editors["ReadOnly"].IsEnabled, "Read-only enum editor must be disabled.");
        grid.CategoryOrder = new[] { "A", "B" };
        grid.PropertyOrder = new[] { "First" };
        Require(Order() == "Other,ReadOnly,First,Second", "Runtime replacement must reorder existing items: " + Order());
        foreach (var item in view.Cast<Hc.PropertyItem>())
            Require(ReferenceEquals(originalItems[item.PropertyName], item) && ReferenceEquals(editors[item.PropertyName], item.EditorElement),
                "Sorting must preserve property items and editor instances.");
        HandyControl.Interactivity.ControlCommands.SortByName.Execute(null, grid);
        Require(Order() == "First,Other,ReadOnly,Second" && view.GroupDescriptions.Count == 0, "Alphabetical sort failed.");
        grid.CategoryOrder = new[] { "B", "A" };
        Require(Order() == "First,Other,ReadOnly,Second" && view.GroupDescriptions.Count == 0, "Changing priority must preserve alphabetical mode.");
        grid.CategoryOrder = null;
        grid.PropertyOrder = null;
        HandyControl.Interactivity.ControlCommands.SortByCategory.Execute(null, grid);
        Require(Order() == "Other,ReadOnly,First,Second", "Null priorities must restore default category/display-name order.");
        Console.WriteLine("PASS PropertyGrid priorities, fallback, runtime sorting, editor identity and enum round-trip.");
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

    public enum TestState
    {
        [System.ComponentModel.Description("Ready description")] Ready,
        Running
    }

    public sealed class SortModel
    {
        [System.ComponentModel.Category("B"), System.ComponentModel.DisplayName("Alpha")]
        public TestState First { get; set; }
        [System.ComponentModel.Category("B"), System.ComponentModel.DisplayName("Zulu")]
        public string Second { get; set; } = "value";
        [System.ComponentModel.Category("A")]
        public string Other { get; set; } = "other";
        [System.ComponentModel.Category("A")]
        public TestState ReadOnly => TestState.Ready;
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
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

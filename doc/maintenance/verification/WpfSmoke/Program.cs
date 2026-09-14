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
    private static int Main()
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

                VerifyNumericUpDown(app);
                VerifyWindowAndGrowl(app);
                Console.WriteLine($"PASS {skin}: NumericUpDown binding, template replacement, limits, WindowChrome, Growl.");
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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

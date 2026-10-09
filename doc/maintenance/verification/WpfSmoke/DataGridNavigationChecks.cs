using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

internal static partial class Program
{
    private static void VerifyDataGridNavigation(Application app, string demoPath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(demoPath))!;
        AssemblyLoadContext.Default.Resolving += (_, name) =>
            File.Exists(Path.Combine(directory, name.Name + ".dll"))
                ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(directory, name.Name + ".dll")) : null;
        var assembly = Assembly.LoadFrom(Path.GetFullPath(demoPath));
        assembly.GetType("HandyControlDemo.Data.GlobalData", true)!.GetMethod("Init")!.Invoke(null, null);
        foreach (var uri in new[] {
            "pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml",
            "pack://application:,,,/HandyControlDemo;component/Resources/Themes/SkinDefault.xaml",
            "pack://application:,,,/HandyControl;component/Themes/Theme.xaml",
            "pack://application:,,,/HandyControlDemo;component/Resources/Themes/Theme.xaml" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(uri) });
        var locator = Activator.CreateInstance(assembly.GetType("HandyControlDemo.ViewModel.ViewModelLocator", true)!)!;
        app.Resources["Locator"] = locator;
        app.Resources["DemoTypes"] = new HandyControl.Data.EnumDataProvider
        {
            Type = assembly.GetType("HandyControlDemo.Data.DemoType", true)!
        };
        var model = locator.GetType().GetProperty("Main")!.GetValue(locator)!;
        var content = (FrameworkElement)Activator.CreateInstance(
            assembly.GetType("HandyControlDemo.UserControl.MainWindowContent", true)!)!;
        content.DataContext = model;
        var window = new Window { Content = content, Width = 1100, Height = 760,
            Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false };
        var source = PresentationTraceSources.DataBindingSource;
        var previousLevel = source.Switch.Level;
        using var output = new StringWriter();
        using var listener = new TextWriterTraceListener(output);
        source.Listeners.Add(listener);
        source.Switch.Level = SourceLevels.Error;
        void Flush()
        {
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
        try
        {
            window.Show();
            var timer = Stopwatch.StartNew();
            ListBox? list = null;
            while (timer.Elapsed < TimeSpan.FromSeconds(10))
            {
                Flush();
                list = FindDescendant<ListBox>(content);
                if (list?.Items.Cast<object>().Any(item =>
                    (string?)item.GetType().GetProperty("TargetCtlName")?.GetValue(item) == "DataGridDemo") == true) break;
            }
            Require(list != null && list.Items.Count > 0, "Demo navigation must load.");
            Console.WriteLine("INITIAL BINDINGS: " + output);
            output.GetStringBuilder().Clear();
            for (var repeat = 0; repeat < 3; repeat++)
            {
                foreach (var target in new[] { "DataGridDemo", "ButtonDemo" })
                {
                    var item = list!.Items.Cast<object>().Single(value =>
                        (string?)value.GetType().GetProperty("TargetCtlName")?.GetValue(value) == target);
                    list.SelectedItem = item;
                    Flush();
                    var page = (FrameworkElement?)model.GetType().GetProperty("SubContent")!.GetValue(model);
                    Require(page?.GetType().Name == target, "Navigation must display " + target);
                    if (target == "DataGridDemo")
                    {
                        var tabs = FindDescendant<TabControl>(page!)!;
                        foreach (var index in new[] { 0, 1, 2, 0 })
                        {
                            tabs.SelectedIndex = index;
                            Flush();
                            var grid = FindDescendant<DataGrid>(page!);
                            Require(grid != null && grid.Items.Count > 0, "Each DataGrid tab must display data.");
                        }
                    }
                }
            }
            listener.Flush();
            Console.WriteLine("NAVIGATION BINDINGS: " + output);
            Require(!output.ToString().Contains("ListBoxItem.HorizontalContentAlignment", StringComparison.Ordinal),
                "Navigation must not produce ListBoxItem.HorizontalContentAlignment binding errors.");
            Console.WriteLine("PASS DataGrid navigation and all three tabs, repeated three times.");
        }
        finally
        {
            window.Close();
            source.Listeners.Remove(listener);
            source.Switch.Level = previousLevel;
        }
    }
}
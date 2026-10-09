using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Hc = HandyControl.Controls;

internal static partial class Program
{
    private static void VerifyFrameDemo(Application app, string demoPath)
    {
        var assembly = Assembly.LoadFrom(System.IO.Path.GetFullPath(demoPath));
        foreach (var skin in new[] { "SkinDefault", "SkinDark", "SkinViolet" })
        {
            app.Resources.MergedDictionaries.Clear();
            foreach (var resource in new[] { skin, "Theme" })
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/HandyControl;component/Themes/{resource}.xaml")
                });
            var demo = (FrameworkElement)Activator.CreateInstance(assembly.GetType("HandyControlDemo.UserControl.FrameDemo", true)!)!;
            var window = new Window { Content = demo, Width = 800, Height = 600,
                Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            void Flush()
            {
                app.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                window.UpdateLayout();
            }
            void Click(Button button)
            {
                Require(button.IsEnabled, "Expected enabled button: " + button.Command);
                ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
                Flush();
            }
            try
            {
                window.Show();
                Flush();
                var frame = (Frame)demo.FindName("frameDemo");
                var group = (Hc.ButtonGroup)frame.Template.FindName("NavChrome", frame);
                var buttons = group.Items.Cast<Button>().ToArray();
                Console.WriteLine($"{skin}: navigation height={group.ActualHeight}, icon={Hc.IconElement.GetWidth(buttons[0])}x{Hc.IconElement.GetHeight(buttons[0])}");
                Require(group.ActualHeight <= 40, "Frame toolbar must not consume the page height: " + group.ActualHeight);
                foreach (var button in buttons)
                {
                    var path = (System.Windows.Shapes.Path)button.Template.FindName("PathMain", button);
                    Require(path.ActualWidth > 0 && path.ActualWidth <= 16.5 && path.ActualHeight > 0 && path.ActualHeight <= 16.5,
                        $"Frame icon must fit within 16 DIP while preserving its aspect ratio: {path.ActualWidth}x{path.ActualHeight}");
                }
                Require(((Page)frame.Content).Title == "0", "Initial page must be 0.");
                Require(!buttons[0].IsEnabled && !buttons[1].IsEnabled, "Initial history buttons must be disabled.");
                Click((Button)((Page)frame.Content).Content);
                Require(((Page)frame.Content).Title == "1", "Page click must navigate to 1.");
                Click((Button)((Page)frame.Content).Content);
                Require(((Page)frame.Content).Title == "2", "Page click must navigate to 2.");
                Click(buttons[0]);
                Require(((Page)frame.Content).Title == "1", "Back must navigate to 1.");
                Click(buttons[1]);
                Require(((Page)frame.Content).Title == "2", "Forward must navigate to 2.");
                Click(buttons[2]);
                Require(((Page)frame.Content).Title == "2", "Refresh must keep page 2.");
                var menu = buttons[0].ContextMenu;
                menu.PlacementTarget = buttons[0];
                menu.IsOpen = true;
                Flush();
                Require(menu.Items.Count == 2, "Back history must contain pages 1 and 0: " + menu.Items.Count);
                var item = (MenuItem)menu.ItemContainerGenerator.ContainerFromIndex(1);
                Require(Equals(item.Header, "0"), "History must display page title 0.");
                var routed = (System.Windows.Input.RoutedCommand)item.Command;
                Require(routed.CanExecute(item.CommandParameter, item.CommandTarget), "History command must be enabled.");
                routed.Execute(item.CommandParameter, item.CommandTarget);
                menu.IsOpen = false;
                Flush();
                Require(((Page)frame.Content).Title == "0", "History selection must navigate to 0.");
                Console.WriteLine("PASS " + skin + ": compiled FrameDemo icon sizing, page navigation, back, forward, refresh and journal menu.");
            }
            finally { window.Close(); }
        }
    }
}

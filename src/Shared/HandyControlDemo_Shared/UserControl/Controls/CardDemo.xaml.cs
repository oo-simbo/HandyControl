using System.Windows;
using System.Windows.Controls;

namespace HandyControlDemo.UserControl;

public partial class CardDemo
{
    public CardDemo()
    {
        InitializeComponent();
    }

    private void HeaderSlotButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string action })
        {
            HeaderSlotActionText.Text = $"Header slot clicks: {action}";
        }
    }
}
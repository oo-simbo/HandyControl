using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace HandyControl.Tools.Helper;

internal class TextHelper
{
    public static FormattedText CreateFormattedText(string text, FlowDirection flowDirection, Typeface typeface, double fontSize)
    {
        var formattedText = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            flowDirection,
            typeface,
            fontSize, Brushes.Black, DpiHelper.DeviceDpiX);

        return formattedText;
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using HandyControl.Data;

internal static partial class Program
{
    private static void VerifyEnumDataProvider()
    {
        var source = PresentationTraceSources.DataBindingSource;
        var previousLevel = source.Switch.Level;
        using var output = new StringWriter();
        using var listener = new TextWriterTraceListener(output);
        source.Listeners.Add(listener);
        source.Switch.Level = SourceLevels.Error;
        try
        {
            var provider = new EnumDataProvider();
            Require(provider.Error is null, "EnumDataProvider construction must not query without a type.");
            provider.Type = typeof(DayOfWeek);
            Require(provider.Error is null && provider.Data is DayOfWeek[] days && days.Length == 7,
                "EnumDataProvider must return all seven weekdays.");
            provider.Type = typeof(ConsoleColor);
            Require(provider.Error is null && provider.Data is ConsoleColor[] colors && colors.Length == 16,
                "Changing Type must replace the argument and data.");
            provider.Type = typeof(ConsoleColor);
            Require(provider.Error is null && provider.MethodParameters.Count == 1,
                "Repeated Type assignment must not accumulate arguments.");

            foreach (var attributesFirst in new[] { true, false })
            {
                var attributed = new EnumDataProvider();
                if (attributesFirst) attributed.UseAttributes = true;
                attributed.Type = typeof(ProviderSample);
                if (!attributesFirst) attributed.UseAttributes = true;
                Require(attributed.Error is null && attributed.Data is IEnumerable<EnumItem> items &&
                    items.Select(item => (item.Value, item.Description)).SequenceEqual(
                        new[] { ((Enum)ProviderSample.Visible, "Visible item") }),
                    "Attribute mode must preserve Description/Browsable and both assignment orders.");
                attributed.UseAttributes = false;
                Require(attributed.Error is null && attributed.Data is ProviderSample[] all && all.Length == 3,
                    "Switching back must return every raw enum value.");
            }

            foreach (var attributes in new[]
            {
                "Type=\"{x:Type sys:DayOfWeek}\"",
                "UseAttributes=\"True\" Type=\"{x:Type sys:DayOfWeek}\"",
                "Type=\"{x:Type sys:DayOfWeek}\" UseAttributes=\"True\""
            })
            {
                var parsed = (EnumDataProvider)XamlReader.Parse($"""
                    <hc:EnumDataProvider xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:sys="clr-namespace:System;assembly=mscorlib"
                        xmlns:hc="https://handyorg.github.io/handycontrol" {attributes} />
                    """);
                Require(parsed.Error is null, "XAML provider initialization must succeed: " + attributes);
                Require(parsed.UseAttributes
                        ? parsed.Data is IEnumerable<EnumItem> values && !values.Any()
                        : parsed.Data is DayOfWeek[] values2 && values2.Length == 7,
                    "XAML attribute order must select the expected data mode: " + attributes);
            }
            listener.Flush();
            Require(output.ToString().Length == 0, "Provider emitted WPF errors: " + output);
            Console.WriteLine("PASS EnumDataProvider construction, repeated Type, attribute order/toggle, filtering, XAML and zero WPF error traces.");
        }
        finally
        {
            source.Listeners.Remove(listener);
            source.Switch.Level = previousLevel;
        }
    }

    public enum ProviderSample
    {
        [Description("Visible item")] Visible,
        [Description("Hidden item"), Browsable(false)] Hidden,
        Undescribed
    }
}

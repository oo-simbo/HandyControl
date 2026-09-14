using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using HandyControl.Data;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace HandyControl.Controls;

public class EnumPropertyEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyItem propertyItem) => new System.Windows.Controls.ComboBox
    {
        IsEnabled = !propertyItem.IsReadOnly,
        ItemsSource = Enum.GetNames(propertyItem.PropertyType).Select(name => new EnumItem
        {
            // Use the field name so aliases retain their own descriptions.
            Value = (Enum) Enum.Parse(propertyItem.PropertyType, name),
            Description = propertyItem.PropertyType.GetField(name)?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name
        }).ToArray(),
        DisplayMemberPath = nameof(EnumItem.Description),
        SelectedValuePath = nameof(EnumItem.Value)
    };

    public override DependencyProperty GetDependencyProperty() => Selector.SelectedValueProperty;
}

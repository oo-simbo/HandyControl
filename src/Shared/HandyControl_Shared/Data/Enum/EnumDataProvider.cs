using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace HandyControl.Data;

public class EnumDataProvider : ObjectDataProvider
{
    private Type _type;
    public Type Type
    {
        get => _type;
        set
        {
            // Refresh only after the target, method and argument are ready.
            using (DeferRefresh())
            {
                _type = value;
                MethodParameters.Clear();
                MethodParameters.Add(value);
                ObjectType = _useAttributes ? typeof(EnumDataProvider) : typeof(System.Enum);
                MethodName = nameof(GetValues);
            }
        }
    }

    private bool _useAttributes;

    public bool UseAttributes
    {
        get => _useAttributes;
        set
        {
            _useAttributes = value;
            if (_type != null)
            {
                ObjectType = value ? typeof(EnumDataProvider) : typeof(System.Enum);
            }
        }
    }

    public static IEnumerable<EnumItem> GetValues(Type enumType)
    {
        if (enumType == null)
        {
            throw new ArgumentNullException(nameof(enumType));
        }

        var resultList = new List<EnumItem>();

        var values = System.Enum.GetValues(enumType);
        foreach (System.Enum value in values)
        {
            var fieldInfo = enumType.GetField(value.ToString());
            if (fieldInfo == null)
            {
                continue;
            }

            if (fieldInfo.GetCustomAttributes(typeof(BrowsableAttribute), true) is BrowsableAttribute[] { Length: > 0 } browsableAttributes)
            {
                if (!browsableAttributes[0].Browsable)
                {
                    continue;
                }
            }

            if (fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true) is DescriptionAttribute[] { Length: > 0 } descriptionAttributes)
            {
                resultList.Add(new EnumItem
                {
                    Description = descriptionAttributes[0].Description,
                    Value = value
                });
            }
        }

        return resultList;
    }
}

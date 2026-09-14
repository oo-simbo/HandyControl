using System;

namespace HandyControl.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DateTimePickerAttribute : Attribute
    {
        public DateTimePickType PickType { get; }
        public DateTimePickerAttribute(DateTimePickType pickType)
        {
            PickType = pickType;
        }
    }

    public enum DateTimePickType
    {
        Both,
        DateOnly,
        TimeOnly
    }
}

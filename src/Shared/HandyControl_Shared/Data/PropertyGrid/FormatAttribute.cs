using System;

namespace HandyControl.Data
{
    /// <summary>
    /// 格式化
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class FormatAttribute : Attribute
    {
        /// <summary>
        /// 格式
        /// </summary>
        public string ValueFormat { get; set; }


        public FormatAttribute(string format)
        {
            ValueFormat = format;
        }
    }
}

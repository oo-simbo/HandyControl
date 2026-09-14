using System.ComponentModel;

namespace HandyControl.Data
{
    /// <summary>
    /// 属性名称排序
    /// </summary>
    public class DisplayNameOrderingAttribute : DisplayNameAttribute
    {
        public DisplayNameOrderingAttribute()
        {

        }

        /// <summary>初始化显示名称和可选排序优先级。</summary>
        /// <param name="displayName">显示名称</param>
        public DisplayNameOrderingAttribute(string displayName) : base(displayName)
        {
        }

        /// <summary>初始化显示名称和可选排序优先级。</summary>
        /// <param name="displayName">显示名称</param>
        /// <param name="order">排序 ，升序排列</param>
        public DisplayNameOrderingAttribute(string displayName, int order) : base(displayName)
        {
            Order = order.ToString().PadLeft(8, '0');
        }

        /// <summary>
        /// 升序排列
        /// </summary>
        public string Order { get; private set; }
    }
}

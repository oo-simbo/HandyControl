using System.ComponentModel;

namespace HandyControl.Data
{
    /// <summary>
    /// 分类排序
    /// </summary>
    public class CategoryOrderingAttribute : CategoryAttribute
    {
        public CategoryOrderingAttribute()
        {

        }

        /// <summary>初始化显示名称和可选排序优先级。</summary>
        /// <param name="category">分类名称</param>
        public CategoryOrderingAttribute(string category) : base(category)
        {
        }

        /// <summary>初始化显示名称和可选排序优先级。</summary>
        /// <param name="category">分类名称</param>
        /// <param name="order">排序，升序排列</param>
        public CategoryOrderingAttribute(string category, int order) : base(category)
        {
            Order = order.ToString().PadLeft(8, '0');
        }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string Order { get; private set; }
    }
}

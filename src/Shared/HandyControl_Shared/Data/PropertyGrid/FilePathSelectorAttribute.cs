using System;

namespace HandyControl.Data
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FilePathSelectorAttribute : Attribute
    {
        public bool IsSelectFolder { get; set; } = true;
        public string? FileFilter { get; set; }
        public string? DialogTitle { get; set; }
        public bool IsTextReadOnly { get; set; } = false;
    }
}

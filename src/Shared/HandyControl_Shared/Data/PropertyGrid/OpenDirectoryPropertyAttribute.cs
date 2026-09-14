using System;

namespace HandyControl.Data;

/// <summary>PropertyGrid directory or file selection metadata.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class OpenDirectoryPropertyAttribute : Attribute
{
    public string? Filter { get; set; }
    public bool IsFile { get; set; }
}

using System.ComponentModel;
using System.Globalization;

namespace SmokeFixtures;

// Test-only external metadata: these types must never be exported by HandyControl.
internal sealed class CategoryOrderingAttribute(string category, int order) : CategoryAttribute(category)
{
    public string Order { get; } = order.ToString("D8", CultureInfo.InvariantCulture);
}

internal sealed class DisplayNameOrderingAttribute(string displayName, int order) : DisplayNameAttribute(displayName)
{
    public string Order { get; } = order.ToString("D8", CultureInfo.InvariantCulture);
}

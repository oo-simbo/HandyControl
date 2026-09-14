using GalaSoft.MvvmLight;

namespace HandyControlDemo.Data;

public class DemoItemModel : ObservableObject
{
    private bool _isVisible = true;
    private string _queriesText = string.Empty;

    public string Name { get; set; }

    public string GroupName { get; set; }

    public string TargetCtlName { get; set; }

    public object ImageBrush { get; set; }

    public bool IsNew { get; set; }

    public string QueriesText
    {
        get => _queriesText;
        set => Set(ref _queriesText, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => Set(ref _isVisible, value);
    }
}

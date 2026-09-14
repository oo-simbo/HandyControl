using System.Collections.Generic;
using GalaSoft.MvvmLight;

namespace HandyControlDemo.Data;

public class DemoInfoModel : ViewModelBase
{
    public string Key { get; set; }

    private string _title;

    public string Title
    {
        get => _title;
        set => Set(ref _title, value);
    }

    private int _selectedIndex;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => Set(ref _selectedIndex, value);
    }

    public bool IsGroupEnabled { get; set; }

    public IList<DemoItemModel> DemoItemList { get; set; }
}

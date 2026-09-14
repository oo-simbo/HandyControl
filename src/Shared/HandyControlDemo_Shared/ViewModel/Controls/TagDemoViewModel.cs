using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HandyControl.Controls;
using HandyControlDemo.Data;
using HandyControlDemo.Properties.Langs;
using HandyControlDemo.Service;

namespace HandyControlDemo.ViewModel;

public class TagDemoViewModel : ViewModelBase
{
    public TagDemoViewModel(DataService dataService)
    {
        DataList = new ObservableCollection<DemoDataModel>(dataService.GetDemoDataList(10));
    }

    public ObservableCollection<DemoDataModel> DataList { get; set; }

    private string _tagName;

    public string TagName
    {
        get => _tagName;
        set => Set(ref _tagName, value);
    }

    public RelayCommand AddItemCmd => new(() =>
    {
        if (string.IsNullOrEmpty(TagName))
        {
            Growl.Warning(Lang.PlsEnterContent);
            return;
        }

        DataList.Insert(0, new DemoDataModel
        {
            IsSelected = DataList.Count % 2 == 0,
            Name = TagName
        });
        TagName = string.Empty;
    });
}

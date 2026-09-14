using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HandyControl.Controls;
using HandyControlDemo.Properties.Langs;
using HandyControlDemo.Tools.Converter;

namespace HandyControlDemo.ViewModel;

public class InputElementDemoViewModel : ViewModelBase
{
    private string _email1;
    private string _email2;
    private string _text1;
    private string _text2;
    private double _doubleValue1;
    private double _doubleValue2;
    private IList<string> _dataList;
    private IList<string> _selectedDataList;

    public string Email1
    {
        get => _email1;
        set => Set(ref _email1, value);
    }

    public string Email2
    {
        get => _email2;
        set => Set(ref _email2, value);
    }

    public string Text1
    {
        get => _text1;
        set => Set(ref _text1, value);
    }

    public string Text2
    {
        get => _text2;
        set => Set(ref _text2, value);
    }

    public double DoubleValue1
    {
        get => _doubleValue1;
        set => Set(ref _doubleValue1, value);
    }

    public double DoubleValue2
    {
        get => _doubleValue2;
        set => Set(ref _doubleValue2, value);
    }

    public IList<string> DataList
    {
        get => _dataList;
        set => Set(ref _dataList, value);
    }

    public IList<string> SelectedDataList
    {
        get => _selectedDataList;
        set => Set(ref _selectedDataList, value);
    }

    public RelayCommand<string> SearchCmd => new(Search);

    public InputElementDemoViewModel()
    {
        DataList = GetComboBoxDemoDataList();
        SelectedDataList = DataList.Where((t, i) => i % 2 == 0).ToList();
    }

    private static void Search(string key)
    {
        Growl.Info(key);
    }

    private static List<string> GetComboBoxDemoDataList()
    {
        var converter = new StringRepeatConverter();
        var list = new List<string>();
        for (var i = 1; i <= 9; i++)
        {
            list.Add($"{converter.Convert(Lang.Text, null, i, CultureInfo.CurrentCulture)}{i}");
        }

        return list;
    }
}

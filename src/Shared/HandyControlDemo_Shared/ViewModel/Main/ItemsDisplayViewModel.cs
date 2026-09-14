using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HandyControlDemo.Data;

namespace HandyControlDemo.ViewModel;

public class ItemsDisplayViewModel : DemoViewModelBase<AvatarModel>
{
    public ItemsDisplayViewModel(Func<List<AvatarModel>> getDataAction)
    {
        Task.Run(() => DataList = getDataAction?.Invoke()).ContinueWith(obj => DataGot = true);
    }

    private bool _dataGot;

    public bool DataGot
    {
        get => _dataGot;
        set => Set(ref _dataGot, value);
    }
}

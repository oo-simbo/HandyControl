using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace HandyControlDemo.ViewModel;

public class BadgeDemoViewModel : ViewModelBase
{
    private int _count = 1;

    public int Count
    {
        get => _count;
        set => Set(ref _count, value);
    }

    public RelayCommand CountCmd => new(() => Count++);
}

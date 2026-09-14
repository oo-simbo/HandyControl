using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HandyControl.Tools.Extension;

namespace HandyControlDemo.ViewModel;

public class InteractiveDialogViewModel : ViewModelBase, IDialogResultable<string>
{
    public Action CloseAction { get; set; }

    private string _result;

    public string Result
    {
        get => _result;
        set => Set(ref _result, value);
    }

    private string _message;

    public string Message
    {
        get => _message;
        set => Set(ref _message, value);
    }

    public RelayCommand CloseCmd => new(() => CloseAction?.Invoke());
}

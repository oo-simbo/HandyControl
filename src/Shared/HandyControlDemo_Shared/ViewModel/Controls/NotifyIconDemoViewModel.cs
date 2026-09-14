using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using HandyControl.Controls;
using HandyControl.Data;
using HandyControlDemo.Data;

namespace HandyControlDemo.ViewModel;

public class NotifyIconDemoViewModel : ViewModelBase
{
    private bool _isCleanup;

    private bool _reversed;

    private string _content = "Hello~~~";

    public string Content
    {
        get => _content;
        set => Set(ref _content, value);
    }

    private bool _contextMenuIsShow;

    public bool ContextMenuIsShow
    {
        get => _contextMenuIsShow;
        set
        {
            Set(ref _contextMenuIsShow, value);
            GlobalData.NotifyIconIsShow = ContextMenuIsShow || ContextContentIsShow;
            if (!_isCleanup && !_reversed)
            {
                _reversed = true;
                ContextContentIsShow = !value;
                _reversed = false;
            }
        }
    }

    private bool _contextMenuIsBlink;

    public bool ContextMenuIsBlink
    {
        get => _contextMenuIsBlink;
        set => Set(ref _contextMenuIsBlink, value);
    }

    private bool _contextContentIsShow;

    public bool ContextContentIsShow
    {
        get => _contextContentIsShow;
        set
        {
            Set(ref _contextContentIsShow, value);
            GlobalData.NotifyIconIsShow = ContextMenuIsShow || ContextContentIsShow;
            if (!_isCleanup && !_reversed)
            {
                _reversed = true;
                ContextMenuIsShow = !value;
                _reversed = false;
            }
        }
    }

    private bool _contextContentIsBlink;

    public bool ContextContentIsBlink
    {
        get => _contextContentIsBlink;
        set => Set(ref _contextContentIsBlink, value);
    }

    public RelayCommand<object> MouseCmd => new(str => Growl.Info(str.ToString()));

    public RelayCommand SendNotificationCmd => new(SendNotification);

    private void SendNotification()
    {
        NotifyIcon.ShowBalloonTip("HandyControl", Content, NotifyIconInfoType.None, ContextMenuIsShow ? MessageToken.NotifyIconDemo : MessageToken.NotifyIconContextDemo);
    }

    public override void Cleanup()
    {
        base.Cleanup();

        _isCleanup = true;
        ContextMenuIsShow = false;
        ContextMenuIsBlink = false;
        ContextContentIsShow = false;
        ContextContentIsBlink = false;
        GlobalData.NotifyIconIsShow = false;
        _isCleanup = false;
    }
}

using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Tools.Extension;
using HandyControlDemo.Data;
using HandyControlDemo.UserControl;
using HandyControlDemo.Window;

namespace HandyControlDemo.ViewModel;

public class DialogDemoViewModel : ViewModelBase
{
    private string _dialogResult;

    public string DialogResult
    {
        get => _dialogResult;
        set => Set(ref _dialogResult, value);
    }

    public RelayCommand<FrameworkElement> ShowTextCmd => new(ShowText);

    private void ShowText(FrameworkElement element)
    {
        if (element == null)
        {
            Dialog.Show(new TextDialog());
        }
        else
        {
            Dialog.Show(new TextDialog(), MessageToken.DialogContainer);
        }
    }

    public RelayCommand<bool> ShowInteractiveDialogCmd => new(async withTimer => await ShowInteractiveDialog(withTimer));

    private async Task ShowInteractiveDialog(bool withTimer)
    {
        if (!withTimer)
        {
            DialogResult = await Dialog.Show<InteractiveDialog>()
                .Initialize<InteractiveDialogViewModel>(vm => vm.Message = DialogResult)
                .GetResultAsync<string>();
        }
        else
        {
            await Dialog.Show<TextDialogWithTimer>(MessageToken.MainWindow).GetResultAsync<string>();
        }
    }

    public RelayCommand NewWindowCmd => new(() => new DialogDemoWindow
    {
        Owner = Application.Current.MainWindow
    }.Show());

    public RelayCommand<string> ShowWithTokenCmd => new(token => Dialog.Show(new TextDialog(), token));

    public RelayCommand<string> CloseMainWindowDialogCmd => new(Dialog.Close);
}

using System.Collections.Generic;
using System.Windows.Forms;

namespace T3.Core.SystemUi;

public class MsForms : ICoreSystemUiService
{
    protected MsForms()
    {
    }

    void ICoreSystemUiService.ShowMessageBox(string text, string caption)
    {
        MessageBox.Show(text, caption);
    }

    PopUpResult ICoreSystemUiService.ShowMessageBox(string text, string caption, PopUpButtons buttons)
    {
        DialogResult result = MessageBox.Show(text, caption, ButtonEnumConversion[buttons]);
        return ResultEnumConversion[result];
    }

    void ICoreSystemUiService.ShowMessageBox(string message)
    {
        MessageBox.Show(message);
    }

    void ICoreSystemUiService.ExitApplication()
    {
        Application.Exit();
    }

    void ICoreSystemUiService.ExitThread()
    {
        Application.ExitThread();
    }

    protected static readonly Dictionary<PopUpButtons, MessageBoxButtons> ButtonEnumConversion =
        new()
            {
                { PopUpButtons.Ok, MessageBoxButtons.OK },
                { PopUpButtons.OkCancel, MessageBoxButtons.OKCancel },
                { PopUpButtons.AbortRetryIgnore, MessageBoxButtons.AbortRetryIgnore },
                { PopUpButtons.YesNoCancel, MessageBoxButtons.YesNoCancel },
                { PopUpButtons.YesNo, MessageBoxButtons.YesNo },
                { PopUpButtons.RetryCancel, MessageBoxButtons.RetryCancel },
                { PopUpButtons.CancelTryContinue, MessageBoxButtons.CancelTryContinue },
            };

    protected static readonly Dictionary<DialogResult, PopUpResult> ResultEnumConversion =
        new()
            {
                { DialogResult.None, PopUpResult.None },
                { DialogResult.OK, PopUpResult.Ok },
                { DialogResult.Cancel, PopUpResult.Cancel },
                { DialogResult.Abort, PopUpResult.Abort },
                { DialogResult.Retry, PopUpResult.Retry },
                { DialogResult.Ignore, PopUpResult.Ignore },
                { DialogResult.Yes, PopUpResult.Yes },
                { DialogResult.No, PopUpResult.No },
                { DialogResult.TryAgain, PopUpResult.TryAgain },
                { DialogResult.Continue, PopUpResult.Continue }
            };
}
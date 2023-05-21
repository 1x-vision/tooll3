using T3.SystemUi;

namespace T3.Core.SystemUi;

public interface ICoreSystemUiService 
{
    public void ShowMessageBox(string text, string caption);
    public PopUpResult ShowMessageBox(string text, string caption, PopUpButtons buttons);
    public void ShowMessageBox(string message);
    public void ExitApplication();
    public void ExitThread();
}
using CloudStatus.Native;
using static CloudStatus.Native.Comctl32;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui.Settings;

internal enum CloseChoice
{
    Cancel,
    MinimizeToTray,
    ExitApplication,
}

/// <summary>
/// Asked when the settings window is closed with the X button: keep running in the tray or exit.
/// Uses a TaskDialog (Common Controls v6) and falls back to a Yes/No/Cancel message box.
/// </summary>
internal static unsafe class CloseConfirmation
{
    const string Title = "CloudStatus";
    const int MinimizeButtonId = 100;
    const int ExitButtonId = 101;

    public static CloseChoice Ask(nint owner) => TryAskWithTaskDialog(owner, out CloseChoice choice) ? choice : AskWithMessageBox(owner);

    static bool TryAskWithTaskDialog(nint owner, out CloseChoice choice)
    {
        choice = CloseChoice.Cancel;
        string minimize = Strings.CloseMinimizeToTray, exit = Strings.CloseExitApplication;
        string instruction = Strings.CloseInstruction, content = Strings.CloseContent;

        fixed (char* minimizeText = minimize, exitText = exit, instructionText = instruction, contentText = content, titleText = Title)
        {
            TASKDIALOG_BUTTON* buttons = stackalloc TASKDIALOG_BUTTON[2];
            buttons[0] = new TASKDIALOG_BUTTON { nButtonID = MinimizeButtonId, pszButtonText = minimizeText };
            buttons[1] = new TASKDIALOG_BUTTON { nButtonID = ExitButtonId, pszButtonText = exitText };

            var config = new TASKDIALOGCONFIG
            {
                cbSize = (uint)sizeof(TASKDIALOGCONFIG),
                hwndParent = owner,
                hInstance = AppInstance.Handle,
                dwFlags = TDF_USE_COMMAND_LINKS | TDF_ALLOW_DIALOG_CANCELLATION | TDF_POSITION_RELATIVE_TO_WINDOW,
                dwCommonButtons = TDCBF_CANCEL_BUTTON,
                pszWindowTitle = titleText,
                pszMainInstruction = instructionText,
                pszContent = contentText,
                cButtons = 2,
                pButtons = buttons,
                nDefaultButton = MinimizeButtonId,
            };

            int pressed = 0;
            try
            {
                if (TaskDialogIndirect(&config, &pressed, null, null) < 0)
                    return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false; // Common Controls v5: no TaskDialog
            }

            choice = pressed switch
            {
                MinimizeButtonId => CloseChoice.MinimizeToTray,
                ExitButtonId => CloseChoice.ExitApplication,
                _ => CloseChoice.Cancel,
            };
            return true;
        }
    }

    static CloseChoice AskWithMessageBox(nint owner) =>
        MessageBoxW(owner, Strings.CloseFallbackQuestion, Title, MB_YESNOCANCEL | MB_ICONQUESTION) switch
        {
            IDYES => CloseChoice.MinimizeToTray,
            IDNO => CloseChoice.ExitApplication,
            _ => CloseChoice.Cancel,
        };
}

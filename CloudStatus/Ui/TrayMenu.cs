using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

internal enum TrayCommand
{
    None = 0,
    Exit = 1,
}

/// <summary>Context menu of the tray icon. It only presents the choice; the caller executes it.</summary>
internal static class TrayMenu
{
    public static TrayCommand Show(nint owner, POINT anchor)
    {
        nint menu = CreatePopupMenu();
        if (menu == 0)
            return TrayCommand.None;

        try
        {
            AppendMenuW(menu, MF_STRING, (nuint)TrayCommand.Exit, Strings.MenuExit);

            // Without these two calls the menu does not close when clicking elsewhere.
            SetForegroundWindow(owner);
            int command = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY, anchor.X, anchor.Y, 0, owner, 0);
            PostMessageW(owner, WM_NULL, 0, 0);
            return (TrayCommand)command;
        }
        finally
        {
            DestroyMenu(menu);
        }
    }
}

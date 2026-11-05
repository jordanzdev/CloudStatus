using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

internal enum TrayCommand
{
    None = 0,
    Settings = 1,
    Exit = 2,
}

/// <summary>
/// Context menu of the tray icon: the state as a disabled line, then the commands (spec 013). It
/// only presents the choice; the caller executes it. Its theme is set by <see cref="SystemTheme"/>.
/// </summary>
internal static class TrayMenu
{
    const uint StatusLineId = 0x100; // disabled, never returned

    public static TrayCommand Show(nint owner, POINT anchor, string statusLine)
    {
        nint menu = CreatePopupMenu();
        if (menu == 0)
            return TrayCommand.None;

        try
        {
            AppendMenuW(menu, MF_STRING | MF_GRAYED, StatusLineId, statusLine);
            AppendMenuW(menu, MF_SEPARATOR, 0, null);
            AppendMenuW(menu, MF_STRING, (nuint)TrayCommand.Settings, Strings.MenuSettings);
            AppendMenuW(menu, MF_SEPARATOR, 0, null);
            AppendMenuW(menu, MF_STRING, (nuint)TrayCommand.Exit, Strings.MenuExit);
            // Bold, as the action a double-click on the icon takes.
            SetMenuDefaultItem(menu, (uint)TrayCommand.Settings, 0);

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

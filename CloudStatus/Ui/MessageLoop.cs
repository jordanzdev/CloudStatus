using CloudStatus.Device;
using CloudStatus.Native;
using static CloudStatus.Native.User32;

namespace CloudStatus.Ui;

/// <summary>
/// The app's only loop. The thread sleeps in MsgWaitForMultipleObjectsEx until a message or an I/O
/// event arrives, so it costs 0 % CPU while idle. No allocations.
/// </summary>
internal static unsafe class MessageLoop
{
    /// <summary>Open window that needs Tab, Enter and Esc navigation through IsDialogMessageW, or 0.</summary>
    public static nint DialogWindow { get; set; }

    /// <summary>Runs until WM_QUIT. Returns false if the wait itself fails.</summary>
    public static bool Run()
    {
        while (true)
        {
            uint handleCount = HidTransport.WaitCount;
            uint result = MsgWaitForMultipleObjectsEx(handleCount, HidTransport.WaitHandles, INFINITE, QS_ALLINPUT, MWMO_INPUTAVAILABLE);
            if (result == WAIT_FAILED)
                return false;
            if (result < handleCount)
                HidTransport.Pump();

            MSG message;
            while (PeekMessageW(&message, 0, 0, 0, PM_REMOVE))
            {
                if (message.message == WM_QUIT)
                    return true;

                nint dialog = DialogWindow;
                if (dialog != 0 && IsDialogMessageW(dialog, &message))
                    continue;
                TranslateMessage(&message);
                DispatchMessageW(&message);
            }
        }
    }
}

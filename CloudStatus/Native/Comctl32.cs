using System.Runtime.InteropServices;

namespace CloudStatus.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct INITCOMMONCONTROLSEX
{
    public uint dwSize;
    public uint dwICC;
}

// commctrl.h declares the TaskDialog structures inside pshpack1: Pack = 1 on x64 too.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TASKDIALOG_BUTTON
{
    public int nButtonID;
    public char* pszButtonText;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TASKDIALOGCONFIG
{
    public uint cbSize;
    public nint hwndParent;
    public nint hInstance;
    public uint dwFlags;
    public uint dwCommonButtons;
    public char* pszWindowTitle;
    public nint hMainIcon;
    public char* pszMainInstruction;
    public char* pszContent;
    public uint cButtons;
    public TASKDIALOG_BUTTON* pButtons;
    public int nDefaultButton;
    public uint cRadioButtons;
    public TASKDIALOG_BUTTON* pRadioButtons;
    public int nDefaultRadioButton;
    public char* pszVerificationText;
    public char* pszExpandedInformation;
    public char* pszExpandedControlText;
    public char* pszCollapsedControlText;
    public nint hFooterIcon;
    public char* pszFooter;
    public nint pfCallback;
    public nint lpCallbackData;
    public uint cxWidth;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NMHDR
{
    public nint hwndFrom;
    public nuint idFrom;
    public uint code;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NMCUSTOMDRAW
{
    public NMHDR hdr;
    public uint dwDrawStage;
    public nint hdc;
    public RECT rc;
    public nuint dwItemSpec;
    public uint uItemState;
    public nint lItemlParam;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct EDITBALLOONTIP
{
    public uint cbStruct;
    public char* pszTitle;
    public char* pszText;
    public int ttiIcon;
}

internal static unsafe partial class Comctl32
{
    const string Lib = "comctl32.dll";

    public const uint EM_SHOWBALLOONTIP = 0x1503; // ECM_FIRST + 3, Common Controls v6
    public const int TTI_WARNING = 2;

    public const uint ICC_STANDARD_CLASSES = 0x00004000;

    public const uint NM_CUSTOMDRAW = unchecked((uint)-12);
    public const uint CDDS_PREPAINT = 0x00000001;
    public const nint CDRF_DODEFAULT = 0x00000000;
    public const nint CDRF_SKIPDEFAULT = 0x00000004;
    public const uint CDIS_SELECTED = 0x0001;
    public const uint CDIS_DISABLED = 0x0004;
    public const uint CDIS_FOCUS = 0x0010;
    public const uint CDIS_HOT = 0x0040;
    public const uint CDIS_SHOWKEYBOARDCUES = 0x0200;

    public const uint TDF_USE_HICON_MAIN = 0x0002;
    public const uint TDF_ALLOW_DIALOG_CANCELLATION = 0x0008;
    public const uint TDF_USE_COMMAND_LINKS = 0x0010;
    public const uint TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000;
    public const uint TDCBF_CANCEL_BUTTON = 0x0008;

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InitCommonControlsEx(INITCOMMONCONTROLSEX* controls);

    [LibraryImport(Lib)]
    public static partial int TaskDialogIndirect(TASKDIALOGCONFIG* config, int* button, int* radioButton, int* verificationChecked);
}

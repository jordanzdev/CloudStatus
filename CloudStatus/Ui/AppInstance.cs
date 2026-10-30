using CloudStatus.Native;

namespace CloudStatus.Ui;

/// <summary>Module handle of the executable, needed to register window classes and create windows.</summary>
internal static class AppInstance
{
    public static readonly nint Handle = Kernel32.GetModuleHandleW(null);
}

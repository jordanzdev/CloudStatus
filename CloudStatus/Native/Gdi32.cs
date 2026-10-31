using System.Runtime.InteropServices;

namespace CloudStatus.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct BITMAPINFOHEADER
{
    public uint biSize;
    public int biWidth;
    public int biHeight;
    public ushort biPlanes;
    public ushort biBitCount;
    public uint biCompression;
    public uint biSizeImage;
    public int biXPelsPerMeter;
    public int biYPelsPerMeter;
    public uint biClrUsed;
    public uint biClrImportant;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SIZE
{
    public int cx;
    public int cy;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct LOGFONTW
{
    public int lfHeight;
    public int lfWidth;
    public int lfEscapement;
    public int lfOrientation;
    public int lfWeight;
    public byte lfItalic;
    public byte lfUnderline;
    public byte lfStrikeOut;
    public byte lfCharSet;
    public byte lfOutPrecision;
    public byte lfClipPrecision;
    public byte lfQuality;
    public byte lfPitchAndFamily;
    public fixed char lfFaceName[32];
}

internal static unsafe partial class Gdi32
{
    const string Lib = "gdi32.dll";

    public const uint BI_RGB = 0;
    public const uint DIB_RGB_COLORS = 0;
    public const int TRANSPARENT = 1;
    public const int FW_BOLD = 700;
    public const byte ANTIALIASED_QUALITY = 4;

    [LibraryImport(Lib)]
    public static partial nint CreateDIBSection(nint hdc, BITMAPINFOHEADER* info, uint usage, void** bits, nint section, uint offset);

    [LibraryImport(Lib)]
    public static partial nint CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, void* bits);

    [LibraryImport(Lib)]
    public static partial nint CreateCompatibleDC(nint hdc);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(nint hdc);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint obj);

    [LibraryImport(Lib)]
    public static partial nint CreateFontIndirectW(LOGFONTW* font);

    [LibraryImport(Lib)]
    public static partial nint SelectObject(nint hdc, nint obj);

    [LibraryImport(Lib)]
    public static partial uint SetTextColor(nint hdc, uint color);

    [LibraryImport(Lib)]
    public static partial int SetBkMode(nint hdc, int mode);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetTextExtentPoint32W(nint hdc, string text, int length, SIZE* size);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GdiFlush();
}

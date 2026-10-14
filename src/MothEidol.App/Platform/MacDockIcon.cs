using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Platform;

namespace MothEidol.App.Platform;

/// <summary>
/// Sets the macOS Dock icon at runtime. When the app runs unbundled (dotnet run / debugger)
/// macOS shows a generic icon because there is no .app bundle to read it from.
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MacDockIcon
{
    private const string ObjC = "/usr/lib/libobjc.dylib";

    [DllImport(ObjC)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjC)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport(ObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr bytes, nuint length);

    public static void TrySet(Uri assetUri)
    {
        try
        {
            using var stream = AssetLoader.Open(assetUri);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var bytes = memory.ToArray();

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                var data = Send(
                    objc_getClass("NSData"),
                    sel_registerName("dataWithBytes:length:"),
                    handle.AddrOfPinnedObject(),
                    (nuint)bytes.Length);

                var image = Send(Send(objc_getClass("NSImage"), sel_registerName("alloc")),
                    sel_registerName("initWithData:"), data);

                if (image == IntPtr.Zero)
                {
                    return;
                }

                var app = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
                Send(app, sel_registerName("setApplicationIconImage:"), image);
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception)
        {
            // Cosmetic only: keep the default Dock icon if anything goes wrong.
        }
    }
}

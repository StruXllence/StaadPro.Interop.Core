using System;
using System.Runtime.InteropServices;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Root.Common
{
    internal interface IRootWindowApi
    {
        bool IsWindow(IntPtr window);
        uint GetProcessId(IntPtr window);
        bool PostShowState(IntPtr window, int command);
    }

    internal sealed class RootWindowApi : IRootWindowApi
    {
        internal static readonly RootWindowApi Instance = new();
        private RootWindowApi() { }
        public bool IsWindow(IntPtr window)
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Window state requests require Windows.");
            return NativeIsWindow(window);
        }
        public uint GetProcessId(IntPtr window) { GetWindowThreadProcessId(window, out uint processId); return processId; }
        public bool PostShowState(IntPtr window, int command) => ShowWindowAsync(window, command);

        [DllImport("user32.dll", EntryPoint = "IsWindow")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool NativeIsWindow(IntPtr window);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindowAsync(IntPtr window, int command);
    }

    internal static class RootWindowControl
    {
        internal static void Request(object root, int command, IRootWindowApi windows)
        {
            ArgumentNullException.ThrowIfNull(windows);
            IntPtr window = ReadHandle((object)((dynamic)root).GetMainWindowHandle());
            uint processId = RootCommonQueryService.GetProcessId(root);
            if (!windows.IsWindow(window) || windows.GetProcessId(window) != processId)
                throw new InvalidOperationException("The reported STAAD window is unavailable or belongs to another process.");
            if (!windows.PostShowState(window, command))
                throw new InvalidOperationException("Windows did not accept the STAAD window-state request.");
        }

        private static IntPtr ReadHandle(object value)
        {
            long number = value is IntPtr pointer ? pointer.ToInt64() : NativeValues.ReadInt64(value, "window handle");
            if (number == 0 || number == -1 || (IntPtr.Size == 4 && (number < int.MinValue || number > uint.MaxValue)))
                throw new InvalidOperationException("OpenSTAAD did not supply a valid pointer-sized window handle.");
            return IntPtr.Size == 4 ? new IntPtr(unchecked((int)number)) : new IntPtr(number);
        }
    }
}

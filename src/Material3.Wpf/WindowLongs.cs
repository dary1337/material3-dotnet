using System;
using System.Runtime.InteropServices;

namespace Material3.Wpf {
    // GetWindowLongPtr only exists in 64-bit user32; a 32-bit process goes through GetWindowLong.
    internal static class WindowLongs {
        internal const int Style = -16;
        internal const int ExStyle = -20;

        internal static long Get(IntPtr hwnd, int index) =>
            IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index).ToInt64() : GetWindowLong32(hwnd, index);

        internal static void Set(IntPtr hwnd, int index, long value) {
            if (IntPtr.Size == 8) {
                SetWindowLongPtr64(hwnd, index, new IntPtr(value));
            }
            else {
                SetWindowLong32(hwnd, index, unchecked((int)value));
            }
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern int GetWindowLong32(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern int SetWindowLong32(IntPtr hwnd, int index, int value);
    }
}

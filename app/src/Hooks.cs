// Hinoki — global low-level hooks (mouse + keyboard)
using System;

namespace Hinoki
{
    /// <summary>Global middle-mouse-button hook. Callbacks arrive on the thread that installed the hook (UI thread).</summary>
    internal static class MouseHook
    {
        /// <summary>Raised on middle-button DOWN with physical screen coordinates.</summary>
        public static event Action<int, int> MiddleDown;

        /// <summary>When true the original middle click is swallowed by Hinoki.</summary>
        public static bool Intercept = true;

        /// <summary>Hard gate — lets the app temporarily ignore the hook (e.g. while a settings dialog captures keys).</summary>
        public static bool Suppressed = false;

        private static IntPtr _hook = IntPtr.Zero;
        private static Native.HookProc _proc;

        public static void Install()
        {
            if (_hook != IntPtr.Zero) return;
            _proc = HookProc;
            IntPtr mod = Native.GetModuleHandle(null);
            _hook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _proc, mod, 0);
        }

        public static void Uninstall()
        {
            if (_hook != IntPtr.Zero) { Native.UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
        }

        private static IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam.ToInt32() == Native.WM_MBUTTONDOWN && !Suppressed)
            {
                Native.MSLLHOOKSTRUCT info = (Native.MSLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(
                    lParam, typeof(Native.MSLLHOOKSTRUCT));
                var handler = MiddleDown;
                if (handler != null) handler(info.pt.X, info.pt.Y);
                if (Intercept) return (IntPtr)1; // consume
            }
            return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
        }
    }

    /// <summary>Keyboard hook, active only while the overlay is open (Esc closes it).</summary>
    internal static class KeyboardHook
    {
        public static event Action EscPressed;

        private static IntPtr _hook = IntPtr.Zero;
        private static Native.HookProc _proc;
        private static int _refs = 0;

        public static void Install()
        {
            _refs++;
            if (_hook != IntPtr.Zero) return;
            _proc = HookProc;
            IntPtr mod = Native.GetModuleHandle(null);
            _hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _proc, mod, 0);
        }

        public static void Uninstall()
        {
            _refs--;
            if (_refs <= 0 && _hook != IntPtr.Zero)
            {
                Native.UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
                _refs = 0;
            }
        }

        private static IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam.ToInt32() == 0x0100) // WM_KEYDOWN
            {
                Native.KBDLLHOOKSTRUCT info = (Native.KBDLLHOOKSTRUCT)System.Runtime.InteropServices.Marshal.PtrToStructure(
                    lParam, typeof(Native.KBDLLHOOKSTRUCT));
                if (info.vkCode == 0x1B) // VK_ESCAPE
                {
                    var h = EscPressed;
                    if (h != null) h();
                }
            }
            return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
        }
    }
}

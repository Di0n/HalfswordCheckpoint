using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HSCheckpoint
{
    internal class HSKeyboardHook : IDisposable
    {
        public event EventHandler? SavePressed;
        public event EventHandler? LoadPressed;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_CONTROL = 0x11;
        private const int WINDOWS_MSG_LOOP_INTERVAL = 10;

        private string procName;
        private Thread? hookThread;
        private IntPtr hookId = IntPtr.Zero;
        private readonly CancellationTokenSource cts;

        // WinAPI imports
        private delegate IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, KeyboardCallback lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public HSKeyboardHook(string procName)
        {
            this.procName = procName;
            cts = new CancellationTokenSource();
        }

        private IntPtr SetHook(KeyboardCallback callback)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, callback, GetModuleHandle(curModule.ModuleName), 0);
        }

        public void Listen()
        {
            KeyboardCallback callback = HookCallback;
            hookThread = new Thread(() =>
            {
                CancellationToken cancellationToken = cts.Token;
                hookId = SetHook(callback);

                // Windows message loop to keep hook alive
                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(WINDOWS_MSG_LOOP_INTERVAL);
                }
                UnhookWindowsHookEx(hookId);
            });
            hookThread.IsBackground = true;
            hookThread.Start();
        }

        public void StopListening()
        {
            cts.Cancel();
            if (hookThread != null && hookThread.IsAlive)
            {
                hookThread.Join(WINDOWS_MSG_LOOP_INTERVAL * 2);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool ctrlDown = vkCode == VK_CONTROL;

                if (ctrlDown && IsGameInForeground())
                {
                    if (vkCode == 'S')
                        SavePressed?.Invoke(this, EventArgs.Empty);
                    else if (vkCode == 'L')
                        LoadPressed?.Invoke(this, EventArgs.Empty);
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private bool IsGameInForeground()
        {
            IntPtr gameHandle = GetForegroundWindow();
            GetWindowThreadProcessId(gameHandle, out uint pid);
            try
            {
                Process proc = Process.GetProcessById((int)pid);
                return proc.ProcessName.Equals(procName, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            cts.Dispose();
        }
    }
}

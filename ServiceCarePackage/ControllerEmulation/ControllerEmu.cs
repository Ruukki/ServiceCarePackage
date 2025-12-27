using Dalamud.Plugin.Services;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using ServiceCarePackage.Services.Logs;
using ServiceCarePackage.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ServiceCarePackage.ControllerEmulation
{
    internal class ControllerEmu : IDisposable
    {
        private readonly IFramework _framework;
        private readonly ChatUI _chatUi;
        private readonly ILog _log;

        // ViGEm
        private readonly ViGEmClient _client = new();
        private readonly IXbox360Controller _pad;

        // State
        private bool _w, _a, _s, _d;
        private double _magnitude = 0.05; // 8% stick: slower than walk

        // Keyboard hook
        private IntPtr _hookId = IntPtr.Zero;

        // Optional: only act when FFXIV is focused
        private const string TargetProcess = "ffxiv_dx11";

        internal ControllerEmu(IFramework framework, ChatUI chatUi, ILog log)
        {
            _framework = framework;
            _chatUi = chatUi;
            _log = log;

            _pad = _client.CreateXbox360Controller(); // factory pattern :contentReference[oaicite:2]{index=2}
            _pad.Connect();


            _framework.Update += OnFrameworkUpdate;
        }

        private void OnFrameworkUpdate(IFramework _)
        {
            if (true || IsGameActive())
            {
                // Always zero stick when chat open (and don’t emulate movement)
                _w = Down(VK_W);
                _a = Down(VK_A);
                _s = Down(VK_S);
                _d = Down(VK_D);
                _log.Debug("OnFrameworkUpdate " + _w.ToString());

                if (_chatUi.IsChatOpen)
                {
                    _log.Debug("Chat is open, skipping emulation");
                    _pad.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                    _pad.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                    return;
                }

                UpdateStick();
            }
        }

        private void UpdateStick()
        {            
            double x = (_d ? 1 : 0) - (_a ? 1 : 0);
            double y = (_w ? 1 : 0) - (_s ? 1 : 0);

            

            if (x == 0 && y == 0)
            {
                _pad.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                _pad.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                return;
            }

            // normalize diagonals
            double len = Math.Sqrt(x * x + y * y);
            x = (x / len) * _magnitude;
            y = (y / len) * _magnitude;

            short sx = (short)Math.Round(x * 32767);
            short sy = (short)Math.Round(y * 32767);

            _pad.SetAxisValue(Xbox360Axis.LeftThumbX, sx);
            _pad.SetAxisValue(Xbox360Axis.LeftThumbY, sy);
        }

        private bool IsGameActive()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;

            GetWindowThreadProcessId(hwnd, out uint pid);
            try
            {
                var p = Process.GetProcessById((int)pid);
                return string.Equals(p.ProcessName, TargetProcess, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public void Dispose()
        {
            _framework.Update -= OnFrameworkUpdate;

            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            // Make sure we stop movement on unload
            try
            {
                _pad.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                _pad.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            }
            catch { }

            _pad.Disconnect();
            _client.Dispose();
        }

        // --- WinAPI ---
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int VK_W = 0x57;
        private const int VK_A = 0x41;
        private const int VK_S = 0x53;
        private const int VK_D = 0x44;

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        static bool Down(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}

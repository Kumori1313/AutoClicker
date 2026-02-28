using AutoClicker.Helpers;
using System.Runtime.InteropServices;

namespace AutoClicker.Services;

public class HotkeyService : IDisposable
{
    private IntPtr _hookId = IntPtr.Zero;
    private NativeMethods.HookProc? _hookProc;
    private uint _registeredKey;
    private bool _isCapturing;
    private bool _disposed;

    public event Action? HotkeyPressed;
    public event Action<uint>? KeyCaptured;

    public uint CurrentKey { get; private set; } = NativeMethods.VK_INSERT;

    public void Start(uint virtualKey)
    {
        Stop();

        CurrentKey = virtualKey;
        _registeredKey = virtualKey;

        // Use low-level keyboard hook for global hotkey detection
        _hookProc = LowLevelKeyboardProc;
        _hookId = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _hookProc,
            NativeMethods.GetModuleHandle(null),
            0);

        if (_hookId == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to set keyboard hook. Error: {Marshal.GetLastWin32Error()}");
        }
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    public void StartCapturing()
    {
        _isCapturing = true;
    }

    public void StopCapturing()
    {
        _isCapturing = false;
    }

    private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN))
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            if (_isCapturing)
            {
                _isCapturing = false;
                KeyCaptured?.Invoke(hookStruct.vkCode);
                return (IntPtr)1; // Consume the key
            }
            else if (hookStruct.vkCode == _registeredKey)
            {
                HotkeyPressed?.Invoke();
                return (IntPtr)1; // Consume the key
            }
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void UpdateKey(uint newKey)
    {
        var wasRunning = _hookId != IntPtr.Zero;
        if (wasRunning)
        {
            Stop();
        }

        CurrentKey = newKey;
        _registeredKey = newKey;

        if (wasRunning)
        {
            Start(newKey);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~HotkeyService()
    {
        Dispose();
    }
}

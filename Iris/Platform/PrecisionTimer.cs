using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Iris.Platform
{
    internal sealed class PrecisionTimer : IDisposable
    {
        private const uint HighResolutionFlag = 0x00000002;
        private const uint TimerAllAccess = 0x1F0003;
        private const uint Infinite = 0xFFFFFFFF;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWaitableTimerExW(IntPtr attributes, string name, uint flags, uint access);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetWaitableTimer(IntPtr timer, ref long dueTime, int period, IntPtr routine, IntPtr arg, bool resume);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        private IntPtr _handle;

        public PrecisionTimer()
        {
            if (OperatingSystem.IsWindows())
                _handle = CreateWaitableTimerExW(IntPtr.Zero, null, HighResolutionFlag, TimerAllAccess);
        }

        public bool IsHighResolution => _handle != IntPtr.Zero;

        public void Wait(double seconds)
        {
            if (seconds <= 0d)
                return;

            if (_handle != IntPtr.Zero)
            {
                long due = -(long)(seconds * 10_000_000d);

                if (due < 0 && SetWaitableTimer(_handle, ref due, 0, IntPtr.Zero, IntPtr.Zero, false))
                {
                    WaitForSingleObject(_handle, Infinite);
                    return;
                }
            }

            Thread.Sleep(1);
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero)
                return;

            CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }
}

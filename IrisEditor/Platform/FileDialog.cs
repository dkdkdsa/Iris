using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace IrisEditor.Platform
{
    [SupportedOSPlatform("windows")]
    internal static class FileDialog
    {
        private const int OfnFileMustExist = 0x1000;
        private const int OfnPathMustExist = 0x800;
        private const int OfnOverwritePrompt = 0x2;
        private const int OfnNoChangeDir = 0x8;

        private const string Filter = "Iris 씬 (*.scene)\0*.scene\0JSON (*.json)\0*.json\0모든 파일 (*.*)\0*.*\0\0";

        private static Thread _thread;
        private static Action<string> _callback;
        private static string _result;
        private static volatile bool _done;

        public static void Open(Action<string> onClosed)
        {
            Begin(owner => Show(owner, open: true), onClosed);
        }

        public static void Save(Action<string> onClosed)
        {
            Begin(owner => Show(owner, open: false), onClosed);
        }

        public static void OpenFolder(Action<string> onClosed)
        {
            Begin(BrowseFolder, onClosed);
        }

        // 매 프레임 호출. 다이얼로그가 닫혔으면 메인 스레드에서 콜백을 실행한다.
        public static void Update()
        {
            if (_thread == null || !_done)
                return;

            var callback = _callback;
            var result = _result;

            _thread = null;
            _callback = null;
            _result = null;
            _done = false;

            callback?.Invoke(result);
        }

        // 셸 다이얼로그는 STA 스레드를 요구한다(메인 스레드는 CLR이 MTA로 굳혀둠).
        // 단, 오너(메인 스레드의 창)가 모달 다이얼로그의 동기 메시지에 응답해야 하므로
        // 메인 스레드를 블록하면 교착된다 — 그래서 폴링 + 콜백 구조다.
        private static void Begin(Func<nint, string> show, Action<string> onClosed)
        {
            if (_thread != null)
                return;

            nint owner = GetActiveWindow();
            _callback = onClosed;
            _done = false;

            _thread = new Thread(() =>
            {
                _result = show(owner);
                _done = true;
            });
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.IsBackground = true;
            _thread.Start();
        }

        private static string Show(nint owner, bool open)
        {
            const int bufferChars = 4096;
            nint buffer = Marshal.AllocHGlobal(bufferChars * sizeof(char));

            try
            {
                Marshal.WriteInt16(buffer, 0, 0);

                var ofn = new OpenFileNameW
                {
                    lStructSize = Marshal.SizeOf<OpenFileNameW>(),
                    hwndOwner = owner,
                    lpstrFilter = Filter,
                    nFilterIndex = 1,
                    lpstrFile = buffer,
                    nMaxFile = bufferChars,
                    lpstrDefExt = "scene",
                    Flags = OfnNoChangeDir | (open ? OfnFileMustExist | OfnPathMustExist : OfnOverwritePrompt),
                };

                bool ok = open ? GetOpenFileNameW(ref ofn) : GetSaveFileNameW(ref ofn);
                return ok ? Marshal.PtrToStringUni(buffer) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string BrowseFolder(nint owner)
        {
            var bi = new BrowseInfoW
            {
                hwndOwner = owner,
                lpszTitle = "워크스페이스 폴더 선택",
                ulFlags = 0x1 | 0x40,
            };

            nint pidl = SHBrowseForFolderW(ref bi);
            if (pidl == 0)
                return null;

            try
            {
                const int bufferChars = 1024;
                nint buffer = Marshal.AllocHGlobal(bufferChars * sizeof(char));

                try
                {
                    Marshal.WriteInt16(buffer, 0, 0);
                    return SHGetPathFromIDListW(pidl, buffer) ? Marshal.PtrToStringUni(buffer) : null;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                CoTaskMemFree(pidl);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileNameW
        {
            public int lStructSize;
            public nint hwndOwner;
            public nint hInstance;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrFilter;
            public nint lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public nint lpstrFile;
            public int nMaxFile;
            public nint lpstrFileTitle;
            public int nMaxFileTitle;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrInitialDir;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpstrDefExt;
            public nint lCustData;
            public nint lpfnHook;
            public nint lpTemplateName;
            public nint pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BrowseInfoW
        {
            public nint hwndOwner;
            public nint pidlRoot;
            public nint pszDisplayName;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszTitle;
            public uint ulFlags;
            public nint lpfn;
            public nint lParam;
            public int iImage;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetOpenFileNameW(ref OpenFileNameW ofn);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetSaveFileNameW(ref OpenFileNameW ofn);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern nint SHBrowseForFolderW(ref BrowseInfoW bi);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SHGetPathFromIDListW(nint pidl, nint path);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(nint pv);

        [DllImport("user32.dll")]
        private static extern nint GetActiveWindow();
    }
}

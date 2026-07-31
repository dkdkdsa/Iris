using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using IrisEditor.Localization;

namespace IrisEditor.Platform
{
    [SupportedOSPlatform("windows")]
    internal static class FileDialog
    {
        private const int OfnFileMustExist = 0x1000;
        private const int OfnPathMustExist = 0x800;
        private const int OfnOverwritePrompt = 0x2;
        private const int OfnNoChangeDir = 0x8;

        private static string Filter => Loc.T("dialog.sceneFilter") + "\0*.scene\0JSON (*.json)\0*.json\0" + Loc.T("dialog.allFilesFilter") + "\0*.*\0\0";

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
                lpszTitle = Loc.T("dialog.pickWorkspace"),
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

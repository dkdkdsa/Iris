using System;
using System.Collections.Generic;
using System.IO;
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
        private const int OfnAllowMultiSelect = 0x200;
        private const int OfnExplorer = 0x80000;

        private const string AssetPatterns =
            "*.png;*.jpg;*.jpeg;*.bmp;*.tga;*.gif;*.wav;*.mp3;*.ttf;*.otf;" +
            "*.sprite;*.tile;*.anim;*.controller;*.ui;*.prefab;*.scene";

        private static string Filter => Loc.T("dialog.sceneFilter") + "\0*.scene\0JSON (*.json)\0*.json\0" + Loc.T("dialog.allFilesFilter") + "\0*.*\0\0";

        private static string AssetFilter => Loc.T("dialog.assetFilter") + "\0" + AssetPatterns + "\0" + Loc.T("dialog.allFilesFilter") + "\0*.*\0\0";

        private static Thread _thread;
        private static Action<string[]> _callback;
        private static string[] _result;
        private static volatile bool _done;

        public static void Open(Action<string> onClosed)
        {
            Begin(owner => Wrap(Show(owner, open: true)), Single(onClosed));
        }

        public static void Save(Action<string> onClosed)
        {
            Begin(owner => Wrap(Show(owner, open: false)), Single(onClosed));
        }

        public static void OpenFolder(Action<string> onClosed)
        {
            Begin(owner => Wrap(BrowseFolder(owner)), Single(onClosed));
        }

        public static void OpenAssets(Action<string[]> onClosed)
        {
            Begin(ShowAssets, onClosed);
        }

        private static string[] Wrap(string path)
        {
            return path == null ? null : new[] { path };
        }

        private static Action<string[]> Single(Action<string> onClosed)
        {
            return paths => onClosed?.Invoke(paths != null && paths.Length > 0 ? paths[0] : null);
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

        private static void Begin(Func<nint, string[]> show, Action<string[]> onClosed)
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

        private static string[] ShowAssets(nint owner)
        {
            const int bufferChars = 32768;
            nint buffer = Marshal.AllocHGlobal(bufferChars * sizeof(char));

            try
            {
                Marshal.WriteInt16(buffer, 0, 0);

                var ofn = new OpenFileNameW
                {
                    lStructSize = Marshal.SizeOf<OpenFileNameW>(),
                    hwndOwner = owner,
                    lpstrFilter = AssetFilter,
                    nFilterIndex = 1,
                    lpstrFile = buffer,
                    nMaxFile = bufferChars,
                    lpstrTitle = Loc.T("dialog.pickAssets"),
                    Flags = OfnNoChangeDir | OfnFileMustExist | OfnPathMustExist | OfnAllowMultiSelect | OfnExplorer,
                };

                return GetOpenFileNameW(ref ofn) ? ParseSelection(buffer, bufferChars) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static string[] ParseSelection(nint buffer, int bufferChars)
        {
            var parts = new List<string>();
            int offset = 0;

            while (offset < bufferChars)
            {
                string part = Marshal.PtrToStringUni(buffer + offset * sizeof(char));

                if (string.IsNullOrEmpty(part))
                    break;

                parts.Add(part);
                offset += part.Length + 1;
            }

            if (parts.Count == 0)
                return null;

            if (parts.Count == 1)
                return new[] { parts[0] };

            var result = new string[parts.Count - 1];

            for (int i = 1; i < parts.Count; i++)
                result[i - 1] = Path.Combine(parts[0], parts[i]);

            return result;
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

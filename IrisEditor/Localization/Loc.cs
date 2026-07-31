using Iris.Debugging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;

namespace IrisEditor.Localization
{
    internal static class Loc
    {
        public const string DefaultLanguage = "en";

        private const int SettleMilliseconds = 250;

        private static readonly Dictionary<string, Dictionary<string, string>> _tables = new(StringComparer.OrdinalIgnoreCase);
        private static readonly SortedSet<string> _missing = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> _windowCache = new(StringComparer.Ordinal);
        private static readonly List<string> _available = new();

        private static Dictionary<string, string> _current = new(StringComparer.Ordinal);
        private static Dictionary<string, string> _fallback = new(StringComparer.Ordinal);
        private static string _directory;
        private static FileSystemWatcher _watcher;
        private static long _reloadAt;

        public static string Language { get; private set; } = DefaultLanguage;

        public static IReadOnlyList<string> Available => _available;

        public static IReadOnlyCollection<string> MissingKeys => _missing;

        public static event Action Changed;

        public static void Load(string directory)
        {
            _directory = directory;
            _tables.Clear();
            _available.Clear();

            if (!Directory.Exists(directory))
            {
                Debug.LogWarning($"Localization directory not found: {directory}");
                Rebind();
                return;
            }

            foreach (var path in Directory.GetFiles(directory, "*.json"))
            {
                string code = Path.GetFileNameWithoutExtension(path);

                if (TryReadTable(path, out var table))
                {
                    _tables[code] = table;
                    _available.Add(code);
                }
            }

            _available.Sort(StringComparer.OrdinalIgnoreCase);
            Rebind();
        }

        public static void Reload()
        {
            if (_directory != null)
                Load(_directory);
        }

        public static void EnableHotReload()
        {
            if (_watcher != null || _directory == null || !Directory.Exists(_directory))
                return;

            try
            {
                _watcher = new FileSystemWatcher(_directory, "*.json")
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                };

                _watcher.Changed += OnFileTouched;
                _watcher.Created += OnFileTouched;
                _watcher.Deleted += OnFileTouched;
                _watcher.Renamed += OnFileTouched;
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                _watcher = null;
                Debug.LogException("Failed to watch localization files", ex);
            }
        }

        public static void PollHotReload()
        {
            long due = Interlocked.Read(ref _reloadAt);

            if (due == 0L || Environment.TickCount64 < due)
                return;

            Interlocked.Exchange(ref _reloadAt, 0L);

            Reload();
            Debug.Log($"Localization reloaded ({Language})");
        }

        private static void OnFileTouched(object sender, FileSystemEventArgs e)
        {
            Interlocked.Exchange(ref _reloadAt, Environment.TickCount64 + SettleMilliseconds);
        }

        public static void SetLanguage(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || string.Equals(code, Language, StringComparison.OrdinalIgnoreCase))
                return;

            Language = code;
            Rebind();
        }

        public static string DisplayName(string code)
        {
            if (_tables.TryGetValue(code, out var table) && table.TryGetValue("language.name", out var name))
                return name;

            return code;
        }

        public static string T(string key)
        {
            if (key == null)
                return string.Empty;

            if (_current.TryGetValue(key, out var value))
                return value;

            if (_fallback.TryGetValue(key, out value))
                return value;

            _missing.Add(key);
            return key;
        }

        public static string T(string key, object arg0)
        {
            return Format(T(key), arg0, null, null, 1);
        }

        public static string T(string key, object arg0, object arg1)
        {
            return Format(T(key), arg0, arg1, null, 2);
        }

        public static string T(string key, object arg0, object arg1, object arg2)
        {
            return Format(T(key), arg0, arg1, arg2, 3);
        }

        public static string Window(string key)
        {
            if (_windowCache.TryGetValue(key, out var cached))
                return cached;

            string title = T(key) + "###" + key;
            _windowCache[key] = title;

            return title;
        }

        public static void ClearMissing()
        {
            _missing.Clear();
        }

        public static bool TryDumpMissing(string path, out string error)
        {
            error = null;

            try
            {
                File.WriteAllLines(path, _missing);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static string Format(string pattern, object arg0, object arg1, object arg2, int count)
        {
            try
            {
                switch (count)
                {
                    case 1: return string.Format(pattern, arg0);
                    case 2: return string.Format(pattern, arg0, arg1);
                    default: return string.Format(pattern, arg0, arg1, arg2);
                }
            }
            catch (FormatException)
            {
                return pattern;
            }
        }

        private static void Rebind()
        {
            _current = _tables.TryGetValue(Language, out var table) ? table : new Dictionary<string, string>(StringComparer.Ordinal);
            _fallback = _tables.TryGetValue(DefaultLanguage, out var english) ? english : new Dictionary<string, string>(StringComparer.Ordinal);

            _windowCache.Clear();
            Changed?.Invoke();
        }

        private static bool TryReadTable(string path, out Dictionary<string, string> table)
        {
            table = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                if (JsonNode.Parse(File.ReadAllText(path)) is not JsonObject root)
                {
                    Debug.LogWarning($"Localization file is not a JSON object: {path}");
                    return false;
                }

                foreach (var pair in root)
                {
                    if (pair.Value is JsonValue value && value.TryGetValue(out string text))
                        table[pair.Key] = text;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException($"Failed to read localization file: {Path.GetFileName(path)}", ex);
                return false;
            }
        }
    }
}

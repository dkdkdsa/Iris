using Hexa.NET.ImGui;
using Iris.Debugging;
using IrisEditor.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace IrisEditor.Panels
{
    internal sealed class ConsolePanel : EditorPanel
    {
        private const int Capacity = 4096;
        private const float DetailHeight = 170f;

        private const uint TraceColor = 0xFF909090;
        private const uint InfoColor = 0xFFDCDCDC;
        private const uint WarningColor = 0xFF50DCFF;
        private const uint ErrorColor = 0xFF5A5AFF;

        private struct Row
        {
            public int Index;
            public int Count;
        }

        private readonly EditorContext _context;
        private readonly BufferLogSink _sink = new(Capacity);
        private readonly LogEntry[] _entries = new LogEntry[Capacity];
        private readonly List<Row> _rows = new();
        private readonly Dictionary<string, int> _collapseIndex = new(StringComparer.Ordinal);
        private readonly List<string> _channels = new();
        private readonly List<string> _stackLines = new();

        private int _entryCount;
        private int _traceCount;
        private int _infoCount;
        private int _warningCount;
        private int _errorCount;

        private int _sinkVersion = -1;
        private int _filterVersion;
        private int _builtFilterVersion = -1;

        private bool _showTrace;
        private bool _showInfo = true;
        private bool _showWarning = true;
        private bool _showError = true;
        private bool _collapse;
        private bool _autoScroll = true;
        private bool _scrollPending;

        private string _search = string.Empty;
        private string _channelFilter;

        private LogEntry _selected;
        private bool _hasSelection;
        private int _selectedRow = -1;

        public ConsolePanel(EditorContext context)
        {
            _context = context;
            Iris.Debugging.Debug.AddSink(_sink);
        }

        public override string Title => "콘솔";

        protected override void OnGui()
        {
            SyncFromSink();
            DrawToolbar();
            DrawList();
            DrawDetail();
        }

        private void SyncFromSink()
        {
            int version = _sink.Version;
            bool hasNewEntries = version != _sinkVersion;

            if (!hasNewEntries && _filterVersion == _builtFilterVersion)
                return;

            _sinkVersion = version;
            _builtFilterVersion = _filterVersion;

            Rebuild();

            if (hasNewEntries)
                _scrollPending = true;
        }

        private void Rebuild()
        {
            _entryCount = _sink.Snapshot(_entries);

            _rows.Clear();
            _collapseIndex.Clear();
            _channels.Clear();

            _traceCount = 0;
            _infoCount = 0;
            _warningCount = 0;
            _errorCount = 0;

            for (int i = 0; i < _entryCount; i++)
            {
                var entry = _entries[i];

                switch (entry.Level)
                {
                    case LogLevel.Trace: _traceCount++; break;
                    case LogLevel.Warning: _warningCount++; break;
                    case LogLevel.Error: _errorCount++; break;
                    default: _infoCount++; break;
                }

                if (!string.IsNullOrEmpty(entry.Channel) && !_channels.Contains(entry.Channel))
                    _channels.Add(entry.Channel);

                if (!Passes(entry))
                    continue;

                if (_collapse)
                {
                    string key = $"{(int)entry.Level}{entry.Channel}{entry.Message}";

                    if (_collapseIndex.TryGetValue(key, out int existing))
                    {
                        var merged = _rows[existing];
                        merged.Index = i;
                        merged.Count++;
                        _rows[existing] = merged;
                        continue;
                    }

                    _collapseIndex[key] = _rows.Count;
                }

                _rows.Add(new Row { Index = i, Count = 1 });
            }

            _channels.Sort(StringComparer.Ordinal);

            if (_selectedRow >= _rows.Count)
                _selectedRow = -1;
        }

        private bool Passes(in LogEntry entry)
        {
            switch (entry.Level)
            {
                case LogLevel.Trace when !_showTrace: return false;
                case LogLevel.Info when !_showInfo: return false;
                case LogLevel.Warning when !_showWarning: return false;
                case LogLevel.Error when !_showError: return false;
            }

            if (_channelFilter != null && entry.Channel != _channelFilter)
                return false;

            if (_search.Length == 0)
                return true;

            return entry.Message != null &&
                   entry.Message.Contains(_search, StringComparison.OrdinalIgnoreCase);
        }

        private void DrawToolbar()
        {
            if (ImGui.Button("지우기"))
            {
                _sink.Clear();
                Iris.Debugging.Debug.ResetOnce();
                _hasSelection = false;
                _selectedRow = -1;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("로그를 비우고 LogOnce 억제 상태도 초기화합니다");

            ImGui.SameLine();

            if (ImGui.Checkbox("합치기", ref _collapse))
                _filterVersion++;

            ImGui.SameLine();
            ImGui.Checkbox("자동 스크롤", ref _autoScroll);

            ImGui.SameLine();
            ImGui.SetNextItemWidth(160f);

            if (ImGui.InputTextWithHint("##ConsoleSearch", "검색...", ref _search, 128))
                _filterVersion++;

            ImGui.SameLine();
            ImGui.SetNextItemWidth(130f);

            if (ImGui.BeginCombo("##ConsoleChannel", _channelFilter ?? "모든 채널"))
            {
                if (ImGui.Selectable("모든 채널", _channelFilter == null))
                {
                    _channelFilter = null;
                    _filterVersion++;
                }

                for (int i = 0; i < _channels.Count; i++)
                {
                    if (ImGui.Selectable(_channels[i], _channelFilter == _channels[i]))
                    {
                        _channelFilter = _channels[i];
                        _filterVersion++;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();
            LevelToggle($"추적 {_traceCount}##ToggleTrace", ref _showTrace, TraceColor);

            ImGui.SameLine();
            LevelToggle($"정보 {_infoCount}##ToggleInfo", ref _showInfo, InfoColor);

            ImGui.SameLine();
            LevelToggle($"경고 {_warningCount}##ToggleWarning", ref _showWarning, WarningColor);

            ImGui.SameLine();
            LevelToggle($"오류 {_errorCount}##ToggleError", ref _showError, ErrorColor);
        }

        private void LevelToggle(string label, ref bool value, uint color)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, color);

            if (ImGui.Checkbox(label, ref value))
                _filterVersion++;

            ImGui.PopStyleColor();
        }

        private void DrawList()
        {
            float available = ImGui.GetContentRegionAvail().Y;
            float height = MathF.Max(60f, available - DetailHeight - ImGui.GetStyle().ItemSpacing.Y);

            if (ImGui.BeginChild("##ConsoleList", new Vector2(0f, height), ImGuiChildFlags.Borders))
            {
                if (_rows.Count == 0)
                    ImGui.TextDisabled(_entryCount == 0 ? "로그가 없습니다" : "필터에 맞는 로그가 없습니다");

                var clipper = new ImGuiListClipper();
                clipper.Begin(_rows.Count);

                while (clipper.Step())
                {
                    for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        DrawRow(i);
                }

                clipper.End();

                if (_scrollPending)
                {
                    _scrollPending = false;

                    if (_autoScroll)
                        ImGui.SetScrollY(ImGui.GetScrollMaxY());
                }
            }

            ImGui.EndChild();
        }

        private void DrawRow(int rowIndex)
        {
            var row = _rows[rowIndex];
            var entry = _entries[row.Index];

            var builder = new StringBuilder();
            builder.Append(entry.TimeUtc.ToLocalTime().ToString("HH:mm:ss.fff"));
            builder.Append("  ");
            builder.Append(LabelOf(entry.Level));
            builder.Append("  ");

            if (!string.IsNullOrEmpty(entry.Channel))
            {
                builder.Append('[');
                builder.Append(entry.Channel);
                builder.Append("] ");
            }

            builder.Append(FirstLine(entry.Message));

            if (row.Count > 1)
            {
                builder.Append("   ×");
                builder.Append(row.Count);
            }

            builder.Append("##ConsoleRow");
            builder.Append(rowIndex);

            ImGui.PushStyleColor(ImGuiCol.Text, ColorOf(entry.Level));

            if (ImGui.Selectable(builder.ToString(), rowIndex == _selectedRow))
            {
                _selectedRow = rowIndex;
                _selected = entry;
                _hasSelection = true;
                CacheStackLines(entry.StackTrace);
            }

            ImGui.PopStyleColor();
        }

        private void DrawDetail()
        {
            if (!ImGui.BeginChild("##ConsoleDetail", new Vector2(0f, 0f), ImGuiChildFlags.Borders))
            {
                ImGui.EndChild();
                return;
            }

            if (!_hasSelection)
            {
                ImGui.TextDisabled("항목을 선택하면 상세 내용이 표시됩니다");
                ImGui.EndChild();
                return;
            }

            if (ImGui.SmallButton("복사"))
                ImGui.SetClipboardText(BuildClipboardText());

            ImGui.SameLine();
            ImGui.TextDisabled(_selected.TimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));

            if (_selected.Context != null)
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"| 컨텍스트: {LogContext.Describe(_selected.Context)}");
            }

            ImGui.Separator();

            ImGui.PushStyleColor(ImGuiCol.Text, ColorOf(_selected.Level));
            ImGui.TextWrapped(_selected.Message ?? string.Empty);
            ImGui.PopStyleColor();

            if (_stackLines.Count > 0)
            {
                ImGui.Separator();

                for (int i = 0; i < _stackLines.Count; i++)
                    DrawStackLine(i);
            }

            ImGui.EndChild();
        }

        private void DrawStackLine(int index)
        {
            string text = _stackLines[index];
            string file = ExtractFile(text, out int lineNumber);

            if (file == null)
            {
                ImGui.TextDisabled(text);
                return;
            }

            if (ImGui.Selectable($"{text}##ConsoleStack{index}"))
                OpenSource(file);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"{file}:{lineNumber} 열기");
        }

        private void CacheStackLines(string stackTrace)
        {
            _stackLines.Clear();

            if (string.IsNullOrEmpty(stackTrace))
                return;

            foreach (var line in stackTrace.Split('\n'))
            {
                string text = line.TrimEnd('\r');

                if (text.Length > 0)
                    _stackLines.Add(text);
            }
        }

        private string BuildClipboardText()
        {
            var builder = new StringBuilder();
            builder.Append(_selected.TimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.Append("  ");
            builder.Append(LabelOf(_selected.Level));

            if (!string.IsNullOrEmpty(_selected.Channel))
            {
                builder.Append("  [");
                builder.Append(_selected.Channel);
                builder.Append(']');
            }

            builder.AppendLine();
            builder.Append(_selected.Message);

            for (int i = 0; i < _stackLines.Count; i++)
            {
                builder.AppendLine();
                builder.Append(_stackLines[i]);
            }

            return builder.ToString();
        }

        private void OpenSource(string file)
        {
            if (!File.Exists(file))
            {
                Iris.Debugging.Debug.Channel("Editor").LogWarning($"소스 파일을 찾을 수 없습니다: {file}");
                return;
            }

            ExternalEditor.OpenScript(_context.Workspace?.ProjectFile, file);
        }

        private static string ExtractFile(string text, out int lineNumber)
        {
            lineNumber = 0;

            int start = text.IndexOf(" in ", StringComparison.Ordinal);

            if (start < 0)
                return null;

            start += 4;

            int marker = text.LastIndexOf(":line ", StringComparison.Ordinal);

            if (marker <= start)
                return null;

            int.TryParse(text.Substring(marker + 6), out lineNumber);

            return text.Substring(start, marker - start);
        }

        private static string FirstLine(string message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            int index = message.IndexOf('\n');

            if (index < 0)
                return message;

            return string.Concat(message.AsSpan(0, index).TrimEnd(), " …");
        }

        private static string LabelOf(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return "TRACE";
                case LogLevel.Warning: return "WARN ";
                case LogLevel.Error: return "ERROR";
                default: return "INFO ";
            }
        }

        private static uint ColorOf(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return TraceColor;
                case LogLevel.Warning: return WarningColor;
                case LogLevel.Error: return ErrorColor;
                default: return InfoColor;
            }
        }
    }
}

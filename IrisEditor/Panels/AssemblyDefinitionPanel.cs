using Hexa.NET.ImGui;
using Iris.Debugging;
using IrisEditor.Localization;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IrisEditor.Panels
{
    internal sealed class AssemblyDefinitionPanel
    {
        private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

        private readonly EditorContext _context;
        private readonly List<string> _references = new();

        private bool _open;
        private EditorWorkspace _workspace;
        private string _path;
        private string _folder;
        private string _name = string.Empty;
        private bool _allowUnsafe;
        private bool _dirty;

        public AssemblyDefinitionPanel(EditorContext context)
        {
            _context = context;
        }

        public void Draw()
        {
            var pending = _context.ConsumePendingAssemblyDefinition();

            if (pending != null)
                Open(pending);

            if (_open && _workspace != _context.Workspace)
                Close();

            if (!_open)
                return;

            ImGui.SetNextWindowSize(new Vector2(420f, 0f), ImGuiCond.FirstUseEver);

            string title = $"{Loc.T("asmdef.title")} - {Path.GetFileName(_path)}{(_dirty ? " *" : "")}###AssemblyDefinitionWindow";

            if (ImGui.Begin(title, ref _open))
                DrawContent();

            ImGui.End();

            if (!_open)
                Close();
        }

        private void Open(string absolutePath)
        {
            var workspace = _context.Workspace;

            if (workspace == null)
                return;

            _references.Clear();
            _name = Path.GetFileNameWithoutExtension(absolutePath);
            _allowUnsafe = false;

            try
            {
                if (JsonNode.Parse(File.ReadAllText(absolutePath)) is JsonObject root)
                {
                    if (root["name"] is JsonValue nameValue && nameValue.TryGetValue(out string name) &&
                        !string.IsNullOrWhiteSpace(name))
                        _name = name;

                    if (root["allowUnsafeCode"] is JsonValue unsafeValue && unsafeValue.TryGetValue(out bool allowUnsafe))
                        _allowUnsafe = allowUnsafe;

                    if (root["references"] is JsonArray references)
                    {
                        foreach (var node in references)
                        {
                            if (node is JsonValue value && value.TryGetValue(out string reference) &&
                                !string.IsNullOrWhiteSpace(reference))
                                _references.Add(reference);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to read assembly definition", ex);
                return;
            }

            _workspace = workspace;
            _path = absolutePath;
            _folder = Path.GetRelativePath(workspace.RootPath, Path.GetDirectoryName(absolutePath)).Replace('\\', '/');
            _dirty = false;
            _open = true;
        }

        private void Close()
        {
            _open = false;
            _workspace = null;
            _path = null;
            _folder = null;
            _references.Clear();
        }

        private void DrawContent()
        {
            ImGui.TextDisabled(Loc.T("asmdef.folder", _folder.Length == 0 ? "/" : _folder));
            ImGui.Separator();

            ImGui.SetNextItemWidth(-140f);
            string name = _name;

            if (ImGui.InputText(Loc.T("asmdef.name"), ref name, 128))
            {
                _name = name;
                _dirty = true;
            }

            bool allowUnsafe = _allowUnsafe;

            if (ImGui.Checkbox(Loc.T("asmdef.allowUnsafe"), ref allowUnsafe))
            {
                _allowUnsafe = allowUnsafe;
                _dirty = true;
            }

            ImGui.Spacing();
            ImGui.SeparatorText(Loc.T("asmdef.references"));

            DrawReferences();

            ImGui.Spacing();
            ImGui.Separator();

            ImGui.BeginDisabled(!_dirty);

            if (ImGui.Button(Loc.T("common.save"), new Vector2(120f, 0f)))
                Save();

            ImGui.SameLine();

            if (ImGui.Button(Loc.T("settings.revert"), new Vector2(120f, 0f)))
                Open(_path);

            ImGui.EndDisabled();

            if (_dirty)
            {
                ImGui.SameLine();
                ImGui.TextDisabled(Loc.T("settings.unsaved"));
            }
        }

        private void DrawReferences()
        {
            var others = new List<string>();

            foreach (var definition in AssemblyDefinitions.Scan(_workspace.RootPath))
            {
                if (!string.Equals(definition.FilePath, _path, StringComparison.OrdinalIgnoreCase) &&
                    !others.Contains(definition.Name))
                    others.Add(definition.Name);
            }

            if (others.Count == 0)
            {
                ImGui.TextDisabled(Loc.T("asmdef.noOthers"));
                return;
            }

            foreach (var other in others)
            {
                bool referenced = _references.Contains(other);

                if (!ImGui.Checkbox(other, ref referenced))
                    continue;

                if (referenced)
                    _references.Add(other);
                else
                    _references.Remove(other);

                _dirty = true;
            }
        }

        private void Save()
        {
            string name = Sanitize(_name);

            if (name.Length == 0)
            {
                Debug.LogWarning(Loc.T("asmdef.nameInvalid"));
                return;
            }

            var references = new JsonArray();

            foreach (var reference in _references)
                references.Add(JsonValue.Create(reference));

            var root = new JsonObject
            {
                ["name"] = JsonValue.Create(name),
                ["references"] = references,
                ["allowUnsafeCode"] = JsonValue.Create(_allowUnsafe),
            };

            try
            {
                File.WriteAllText(_path, root.ToJsonString(_writeOptions));
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to write assembly definition", ex);
                return;
            }

            _name = name;
            _dirty = false;

            _workspace.Refresh();
            _context.RefreshAssemblies();
        }

        private static string Sanitize(string value)
        {
            var builder = new System.Text.StringBuilder();

            foreach (char c in (value ?? string.Empty).Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '.' || c == '_')
                    builder.Append(c);
            }

            return builder.ToString();
        }
    }
}

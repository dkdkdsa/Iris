using Hexa.NET.ImGui;
using Iris.Assets;
using Iris.Core;
using Iris.Debugging;
using StbiSharp;
using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using IrisEditor.Localization;

namespace IrisEditor.Panels
{
    internal sealed unsafe class SpriteSlicerPanel
    {
        private readonly EditorContext _context;

        private bool _open;
        private string _texturePath;
        private ITexture _texture;
        private byte[] _pixels;
        private int _pixelWidth;
        private int _pixelHeight;

        private int _cellWidth = 16;
        private int _cellHeight = 16;
        private int _offsetX;
        private int _offsetY;
        private string _name = string.Empty;
        private float _fps = 12f;
        private bool _loop = true;

        private bool[] _emptyCells = Array.Empty<bool>();
        private int _columns;
        private int _rows;
        private bool _gridDirty = true;

        public SpriteSlicerPanel(EditorContext context)
        {
            _context = context;
        }

        public void Draw()
        {
            var pending = _context.ConsumePendingSpriteSlicer();

            if (pending != null)
                Open(pending);

            if (!_open)
                return;

            ImGui.SetNextWindowSize(new Vector2(900f, 620f), ImGuiCond.FirstUseEver);

            string title = $"{Loc.T("slicer.title")} - {Path.GetFileName(_texturePath)}###SpriteSlicerWindow";

            if (ImGui.Begin(title, ref _open))
                DrawContent();

            ImGui.End();

            if (!_open)
                _pixels = null;
        }

        private void Open(string relativePath)
        {
            var workspace = _context.Workspace;

            if (workspace == null)
                return;

            try
            {
                var bytes = File.ReadAllBytes(workspace.ToAbsolute(relativePath));

                using (var image = Stbi.LoadFromMemory(bytes, 4))
                {
                    _pixels = image.Data.ToArray();
                    _pixelWidth = image.Width;
                    _pixelHeight = image.Height;
                }

                _texture = AssetManager.Load<ITexture>(Path.Combine(workspace.RootPath, relativePath));
                _texturePath = relativePath;
                _name = Path.GetFileNameWithoutExtension(relativePath);
                _gridDirty = true;
                _open = true;
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to open image", ex);
            }
        }

        private void DrawContent()
        {
            RefreshCells();

            ImGui.BeginChild("SlicerParams", new Vector2(240f, 0f), ImGuiChildFlags.Borders);

            ImGui.TextDisabled(Loc.T("slicer.source", _pixelWidth, _pixelHeight));
            ImGui.Separator();

            ImGui.SetNextItemWidth(120f);
            _gridDirty |= ImGui.InputInt(Loc.T("slicer.cellWidth"), ref _cellWidth);
            ImGui.SetNextItemWidth(120f);
            _gridDirty |= ImGui.InputInt(Loc.T("slicer.cellHeight"), ref _cellHeight);
            ImGui.SetNextItemWidth(120f);
            _gridDirty |= ImGui.InputInt(Loc.T("slicer.offsetX"), ref _offsetX);
            ImGui.SetNextItemWidth(120f);
            _gridDirty |= ImGui.InputInt(Loc.T("slicer.offsetY"), ref _offsetY);

            _cellWidth = Math.Max(1, _cellWidth);
            _cellHeight = Math.Max(1, _cellHeight);
            _offsetX = Math.Clamp(_offsetX, 0, Math.Max(0, _pixelWidth - 1));
            _offsetY = Math.Clamp(_offsetY, 0, Math.Max(0, _pixelHeight - 1));

            int emptyCount = 0;

            foreach (var empty in _emptyCells)
            {
                if (empty)
                    emptyCount++;
            }

            ImGui.TextDisabled(Loc.T("slicer.grid", _columns, _rows, _columns * _rows));
            ImGui.TextDisabled(Loc.T("slicer.emptyCells", emptyCount));

            ImGui.Separator();

            ImGui.SetNextItemWidth(-1f);
            ImGui.InputText("##SliceName", ref _name, 64);

            if (ImGui.Button(Loc.T("slicer.sliceSprites"), new Vector2(-1f, 0f)))
                ExportSlices(".sprite");

            if (ImGui.Button(Loc.T("slicer.sliceTiles"), new Vector2(-1f, 0f)))
                ExportSlices(".tile");

            ImGui.Separator();

            ImGui.SetNextItemWidth(120f);
            ImGui.InputFloat("FPS", ref _fps);
            ImGui.Checkbox(Loc.T("slicer.loop"), ref _loop);

            if (ImGui.Button(Loc.T("slicer.animAll"), new Vector2(-1f, 0f)))
                ExportAnimation();

            if (ImGui.Button(Loc.T("slicer.animPerRow"), new Vector2(-1f, 0f)))
                ExportRowAnimations();

            if (ImGui.Button(Loc.T("slicer.animPerColumn"), new Vector2(-1f, 0f)))
                ExportColumnAnimations();

            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("SlicerPreview", Vector2.Zero, ImGuiChildFlags.Borders);
            DrawPreview();
            ImGui.EndChild();
        }

        private void DrawPreview()
        {
            if (_texture == null || _pixelWidth <= 0 || _pixelHeight <= 0)
            {
                ImGui.TextDisabled(Loc.T("slicer.noTexture"));
                return;
            }

            var avail = ImGui.GetContentRegionAvail();

            if (avail.X < 8f || avail.Y < 8f)
                return;

            float scale = MathF.Min(avail.X / _pixelWidth, avail.Y / _pixelHeight) * 0.96f;
            var drawSize = new Vector2(_pixelWidth, _pixelHeight) * scale;
            var origin = ImGui.GetCursorScreenPos() + (avail - drawSize) * 0.5f;

            var drawList = ImGui.GetWindowDrawList();

            drawList.AddRectFilled(origin - Vector2.One, origin + drawSize + Vector2.One, 0xFF151515);
            drawList.AddImage(new ImTextureRef(null, _texture.Handle), origin, origin + drawSize);

            for (int cy = 0; cy < _rows; cy++)
            {
                for (int cx = 0; cx < _columns; cx++)
                {
                    if (!_emptyCells[cy * _columns + cx])
                        continue;

                    var min = origin + new Vector2(_offsetX + cx * _cellWidth, _offsetY + cy * _cellHeight) * scale;
                    var max = min + new Vector2(_cellWidth, _cellHeight) * scale;
                    drawList.AddRectFilled(min, max, 0x300000FF);
                }
            }

            uint lineColor = 0x8000FF80;

            for (int cx = 0; cx <= _columns; cx++)
            {
                float x = origin.X + (_offsetX + cx * _cellWidth) * scale;
                drawList.AddLine(new Vector2(x, origin.Y + _offsetY * scale),
                    new Vector2(x, origin.Y + (_offsetY + _rows * _cellHeight) * scale), lineColor);
            }

            for (int cy = 0; cy <= _rows; cy++)
            {
                float y = origin.Y + (_offsetY + cy * _cellHeight) * scale;
                drawList.AddLine(new Vector2(origin.X + _offsetX * scale, y),
                    new Vector2(origin.X + (_offsetX + _columns * _cellWidth) * scale, y), lineColor);
            }
        }

        private void RefreshCells()
        {
            if (!_gridDirty)
                return;

            _gridDirty = false;

            _columns = _cellWidth > 0 ? Math.Max(0, (_pixelWidth - _offsetX) / _cellWidth) : 0;
            _rows = _cellHeight > 0 ? Math.Max(0, (_pixelHeight - _offsetY) / _cellHeight) : 0;
            _emptyCells = new bool[_columns * _rows];

            for (int cy = 0; cy < _rows; cy++)
            {
                for (int cx = 0; cx < _columns; cx++)
                    _emptyCells[cy * _columns + cx] = !CellHasPixels(cx, cy);
            }
        }

        private bool CellHasPixels(int cellX, int cellY)
        {
            if (_pixels == null)
                return true;

            int startX = _offsetX + cellX * _cellWidth;
            int startY = _offsetY + cellY * _cellHeight;
            int endX = Math.Min(startX + _cellWidth, _pixelWidth);
            int endY = Math.Min(startY + _cellHeight, _pixelHeight);

            for (int y = startY; y < endY; y++)
            {
                int row = y * _pixelWidth * 4;

                for (int x = startX; x < endX; x++)
                {
                    if (_pixels[row + x * 4 + 3] > 0)
                        return true;
                }
            }

            return false;
        }

        private void ExportSlices(string extension)
        {
            var workspace = _context.Workspace;

            if (workspace == null || _texturePath == null)
                return;

            RefreshCells();

            try
            {
                string directory = Path.GetDirectoryName(workspace.ToAbsolute(_texturePath));
                int exported = 0;

                for (int cy = 0; cy < _rows; cy++)
                {
                    for (int cx = 0; cx < _columns; cx++)
                    {
                        int index = cy * _columns + cx;

                        if (_emptyCells[index])
                            continue;

                        var json = new JsonObject
                        {
                            ["texture"] = _texturePath,
                            ["x"] = _offsetX + cx * _cellWidth,
                            ["y"] = _offsetY + cy * _cellHeight,
                            ["width"] = _cellWidth,
                            ["height"] = _cellHeight,
                        };

                        File.WriteAllText(Path.Combine(directory, $"{_name}_{index}{extension}"),
                            json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                        exported++;
                    }
                }

                workspace.Refresh();
                Debug.Log($"Created {exported} {extension} assets");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to slice", ex);
            }
        }

        private void ExportAnimation()
        {
            var workspace = _context.Workspace;

            if (workspace == null || _texturePath == null)
                return;

            RefreshCells();

            int frameCount = 0;

            for (int i = 0; i < _emptyCells.Length; i++)
            {
                if (!_emptyCells[i])
                    frameCount = i + 1;
            }

            if (frameCount == 0)
            {
                Debug.LogWarning("No frames to slice.");
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(workspace.ToAbsolute(_texturePath));

                WriteAnim(directory, $"{_name}.anim", _offsetX, _offsetY, _columns, frameCount);

                workspace.Refresh();
                Debug.Log($"Animation created: {_name}.anim");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to create animation", ex);
            }
        }

        private void ExportRowAnimations()
        {
            var workspace = _context.Workspace;

            if (workspace == null || _texturePath == null)
                return;

            RefreshCells();

            try
            {
                string directory = Path.GetDirectoryName(workspace.ToAbsolute(_texturePath));
                int exported = 0;

                for (int cy = 0; cy < _rows; cy++)
                {
                    int frameCount = 0;

                    for (int cx = 0; cx < _columns; cx++)
                    {
                        if (!_emptyCells[cy * _columns + cx])
                            frameCount = cx + 1;
                    }

                    if (frameCount == 0)
                        continue;

                    WriteAnim(directory, $"{_name}_row{cy}.anim",
                        _offsetX, _offsetY + cy * _cellHeight, _columns, frameCount);
                    exported++;
                }

                workspace.Refresh();
                Debug.Log($"Created {exported} row animations");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to create animation", ex);
            }
        }

        private void ExportColumnAnimations()
        {
            var workspace = _context.Workspace;

            if (workspace == null || _texturePath == null)
                return;

            RefreshCells();

            try
            {
                string directory = Path.GetDirectoryName(workspace.ToAbsolute(_texturePath));
                int exported = 0;

                for (int cx = 0; cx < _columns; cx++)
                {
                    int frameCount = 0;

                    for (int cy = 0; cy < _rows; cy++)
                    {
                        if (!_emptyCells[cy * _columns + cx])
                            frameCount = cy + 1;
                    }

                    if (frameCount == 0)
                        continue;

                    WriteAnim(directory, $"{_name}_col{cx}.anim",
                        _offsetX + cx * _cellWidth, _offsetY, 1, frameCount);
                    exported++;
                }

                workspace.Refresh();
                Debug.Log($"Created {exported} column animations");
            }
            catch (Exception ex)
            {
                Debug.LogException("Failed to create animation", ex);
            }
        }

        private void WriteAnim(string directory, string fileName, int offsetX, int offsetY, int columns, int frameCount)
        {
            var json = new JsonObject
            {
                ["texture"] = _texturePath,
                ["fps"] = _fps,
                ["loop"] = _loop,
                ["frameWidth"] = _cellWidth,
                ["frameHeight"] = _cellHeight,
                ["frameCount"] = frameCount,
                ["offsetX"] = offsetX,
                ["offsetY"] = offsetY,
                ["columns"] = columns,
            };

            File.WriteAllText(Path.Combine(directory, fileName),
                json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}

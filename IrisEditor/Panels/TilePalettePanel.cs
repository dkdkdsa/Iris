using Hexa.NET.ImGui;
using Iris.Assets;
using Iris.Core;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using IrisEditor.Localization;

namespace IrisEditor.Panels
{
    internal static class TileBrush
    {
        public static string TilePath;
        public static bool Eraser;

        public static bool Active => Eraser || !string.IsNullOrEmpty(TilePath);
    }

    internal sealed unsafe class TilePalettePanel : EditorPanel
    {
        private const float ThumbSize = 48f;

        private readonly EditorContext _context;
        private static readonly HashSet<string> _broken = new();
        private bool _focusPending;

        public TilePalettePanel(EditorContext context)
        {
            _context = context;
            IsOpen = false;
        }

        public override string Title => Loc.Window("panel.tile");

        public void Open()
        {
            IsOpen = true;
            _focusPending = true;
        }

        public override void Draw()
        {
            if (!IsOpen && TileBrush.Active)
            {
                TileBrush.TilePath = null;
                TileBrush.Eraser = false;
            }

            base.Draw();
        }

        protected override void OnGui()
        {
            if (_focusPending)
            {
                _focusPending = false;
                ImGui.SetWindowFocus();
            }

            var workspace = _context.Workspace;

            if (workspace == null)
            {
                ImGui.TextDisabled(Loc.T("common.noProject"));
                return;
            }

            if (ImGui.Selectable(Loc.T("tile.selectTool"), !TileBrush.Active))
            {
                TileBrush.TilePath = null;
                TileBrush.Eraser = false;
            }

            if (ImGui.Selectable(Loc.T("tile.eraser"), TileBrush.Eraser))
            {
                TileBrush.Eraser = !TileBrush.Eraser;

                if (TileBrush.Eraser)
                    TileBrush.TilePath = null;
            }

            var selected = _context.Selected;
            bool hasTilemap = selected?.GetComponent(typeof(Tilemap)) != null;

            if (TileBrush.Active && !hasTilemap)
                ImGui.TextDisabled(Loc.T("tile.needTilemap"));

            if (hasTilemap && selected.GetComponent(typeof(TilemapRenderer)) == null)
                ImGui.TextDisabled(Loc.T("tile.noRenderer"));

            ImGui.Separator();

            ImGui.BeginChild("TilePaletteList");

            var style = ImGui.GetStyle();
            float availWidth = ImGui.GetContentRegionAvail().X;
            int columns = Math.Max(1, (int)((availWidth + style.ItemSpacing.X) / (ThumbSize + style.ItemSpacing.X)));
            int column = 0;
            int count = 0;

            foreach (var asset in workspace.Assets)
            {
                if (asset.AssetType != typeof(Tile))
                    continue;

                count++;

                if (column > 0)
                    ImGui.SameLine();

                column = (column + 1) % columns;

                if (DrawTileItem(workspace, asset))
                {
                    TileBrush.TilePath = asset.Path;
                    TileBrush.Eraser = false;
                }
            }

            if (count == 0)
                ImGui.TextDisabled(Loc.T("tile.noTileAssets"));

            ImGui.EndChild();
        }

        private static bool DrawTileItem(EditorWorkspace workspace, AssetEntry asset)
        {
            ImGui.PushID(asset.Path);
            ImGui.BeginGroup();

            bool clicked = ImGui.Button("##TileThumb", new Vector2(ThumbSize, ThumbSize));

            var drawList = ImGui.GetWindowDrawList();
            var thumbMin = ImGui.GetItemRectMin();
            var tile = LoadTile(workspace, asset.Path);

            if (tile?.Texture != null && tile.Texture.Width > 0 && tile.Texture.Height > 0)
            {
                float texWidth = tile.Texture.Width;
                float texHeight = tile.Texture.Height;

                var uv0 = Vector2.Zero;
                var uv1 = Vector2.One;
                float pixelWidth = texWidth;
                float pixelHeight = texHeight;

                if (tile.SrcRect is { } src)
                {
                    uv0 = new Vector2(src.Origin.X / texWidth, src.Origin.Y / texHeight);
                    uv1 = new Vector2((src.Origin.X + src.Size.X) / texWidth, (src.Origin.Y + src.Size.Y) / texHeight);
                    pixelWidth = src.Size.X;
                    pixelHeight = src.Size.Y;
                }

                if (pixelWidth > 0f && pixelHeight > 0f)
                {
                    float scale = MathF.Min(ThumbSize / pixelWidth, ThumbSize / pixelHeight) * 0.92f;
                    var size = new Vector2(pixelWidth, pixelHeight) * scale;
                    var offset = (new Vector2(ThumbSize, ThumbSize) - size) * 0.5f;

                    drawList.AddImage(new ImTextureRef(null, tile.Texture.Handle), thumbMin + offset, thumbMin + offset + size, uv0, uv1);
                }
            }
            else
            {
                var center = thumbMin + new Vector2(ThumbSize, ThumbSize) * 0.5f;
                var textSize = ImGui.CalcTextSize("?");
                drawList.AddText(center - textSize * 0.5f, ImGui.GetColorU32(ImGuiCol.TextDisabled), "?");
            }

            ImGui.Text(Truncate(Path.GetFileNameWithoutExtension(asset.Path), ThumbSize));
            ImGui.EndGroup();

            if (TileBrush.TilePath == asset.Path)
                drawList.AddRect(ImGui.GetItemRectMin() - Vector2.One, ImGui.GetItemRectMax() + Vector2.One,
                    ImGui.GetColorU32(ImGuiCol.CheckMark));

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(asset.Path);

            ImGui.PopID();
            return clicked;
        }

        private static Tile LoadTile(EditorWorkspace workspace, string relativePath)
        {
            if (_broken.Contains(relativePath))
                return null;

            try
            {
                return AssetManager.Load<Tile>(Path.Combine(workspace.RootPath, relativePath));
            }
            catch
            {
                _broken.Add(relativePath);
                return null;
            }
        }

        private static string Truncate(string text, float width)
        {
            if (ImGui.CalcTextSize(text).X <= width)
                return text;

            while (text.Length > 1 && ImGui.CalcTextSize(text + "..").X > width)
                text = text[..^1];

            return text + "..";
        }
    }
}

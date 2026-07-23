using Hexa.NET.ImGui;
using Iris.Assets;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Panels
{
    internal static unsafe class AssetPicker
    {
        private const string PopupId = "AssetPickerPopup";
        private const float ThumbSize = 64f;

        private static string _search = string.Empty;
        private static readonly HashSet<string> _brokenThumbnails = new();

        public static JsonNode Draw(string label, string current, Type assetType, EditorWorkspace workspace)
        {
            JsonNode result = null;

            ImGui.PushID(label);

            var style = ImGui.GetStyle();
            float frameHeight = ImGui.GetFrameHeight();
            float fieldWidth = MathF.Max(ImGui.CalcItemWidth() - frameHeight - style.ItemInnerSpacing.X, frameHeight);

            bool hasValue = !string.IsNullOrEmpty(current);
            string display = hasValue
                ? Path.GetFileNameWithoutExtension(current)
                : $"없음 ({TypeDisplayName(assetType)})";

            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.FrameBg));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetColorU32(ImGuiCol.FrameBgHovered));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetColorU32(ImGuiCol.FrameBgActive));

            bool openPopup = ImGui.Button("##AssetField", new Vector2(fieldWidth, frameHeight));

            ImGui.PopStyleColor(3);

            DrawFieldContent(display, hasValue ? current : null, assetType, workspace);

            if (hasValue && ImGui.IsItemHovered())
                ImGui.SetTooltip(current);

            if (ImGui.BeginDragDropTarget())
            {
                var payload = ImGui.AcceptDragDropPayload(AssetDragDrop.PayloadType);

                if (!payload.IsNull && AssetDragDrop.Current != null &&
                    assetType.IsAssignableFrom(AssetDragDrop.Current.AssetType))
                    result = JsonValue.Create(AssetDragDrop.Current.Path);

                ImGui.EndDragDropTarget();
            }

            ImGui.SameLine(0f, style.ItemInnerSpacing.X);

            openPopup |= ImGui.Button("##AssetPickerButton", new Vector2(frameHeight, frameHeight));
            DrawPickerIcon();

            ImGui.SameLine(0f, style.ItemInnerSpacing.X);
            ImGui.Text(label);

            if (openPopup)
            {
                _search = string.Empty;
                ImGui.OpenPopup(PopupId);
            }

            result = DrawPopup(current, assetType, workspace) ?? result;

            ImGui.PopID();
            return result;
        }

        private static void DrawFieldContent(string display, string currentPath, Type assetType, EditorWorkspace workspace)
        {
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            var drawList = ImGui.GetWindowDrawList();
            var style = ImGui.GetStyle();

            float textX = min.X + style.FramePadding.X;

            if (currentPath != null && typeof(ITexture).IsAssignableFrom(assetType))
            {
                var thumb = LoadThumbnail(workspace, currentPath);

                if (thumb != null && thumb.Width > 0 && thumb.Height > 0)
                {
                    float box = max.Y - min.Y - 4f;
                    float scale = MathF.Min(box / thumb.Width, box / thumb.Height);
                    var size = new Vector2(thumb.Width, thumb.Height) * scale;
                    var iconMin = new Vector2(textX, min.Y + 2f) + (new Vector2(box, box) - size) * 0.5f;

                    drawList.AddImage(new ImTextureRef(null, thumb.Handle), iconMin, iconMin + size);
                    textX += box + style.ItemInnerSpacing.X;
                }
            }

            float textY = min.Y + (max.Y - min.Y - ImGui.GetFontSize()) * 0.5f;
            uint color = ImGui.GetColorU32(currentPath != null ? ImGuiCol.Text : ImGuiCol.TextDisabled);

            drawList.PushClipRect(min, new Vector2(max.X - style.FramePadding.X, max.Y), true);
            drawList.AddText(new Vector2(textX, textY), color, display);
            drawList.PopClipRect();
        }

        private static void DrawPickerIcon()
        {
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            var center = (min + max) * 0.5f;
            float radius = (max.Y - min.Y) * 0.24f;
            uint color = ImGui.GetColorU32(ImGuiCol.Text);

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddCircle(center, radius, color, 0, 1.5f);
            drawList.AddCircleFilled(center, radius * 0.4f, color);
        }

        private static JsonNode DrawPopup(string current, Type assetType, EditorWorkspace workspace)
        {
            ImGui.SetNextWindowSize(new Vector2(420f, 460f), ImGuiCond.Appearing);

            if (!ImGui.BeginPopup(PopupId))
                return null;

            JsonNode result = null;

            ImGui.TextDisabled($"{TypeDisplayName(assetType)} 선택");
            ImGui.Separator();

            if (ImGui.IsWindowAppearing())
                ImGui.SetKeyboardFocusHere();

            ImGui.SetNextItemWidth(-1f);
            ImGui.InputTextWithHint("##AssetSearch", "검색...", ref _search, 128);

            ImGui.Spacing();

            ImGui.BeginChild("AssetPickerList");

            if (workspace == null)
            {
                ImGui.TextDisabled("(열린 프로젝트가 없습니다)");
            }
            else
            {
                if (ImGui.Selectable("(없음)", string.IsNullOrEmpty(current)))
                {
                    result = JsonValue.Create(string.Empty);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.Spacing();

                bool useGrid = typeof(ITexture).IsAssignableFrom(assetType);

                var style = ImGui.GetStyle();
                float availWidth = ImGui.GetContentRegionAvail().X;
                int columns = Math.Max(1, (int)((availWidth + style.ItemSpacing.X) / (ThumbSize + style.ItemSpacing.X)));

                int column = 0;
                int matches = 0;

                foreach (var asset in workspace.Assets)
                {
                    if (asset.AssetType == null || !assetType.IsAssignableFrom(asset.AssetType))
                        continue;

                    string name = Path.GetFileNameWithoutExtension(asset.Path);

                    if (_search.Length > 0 &&
                        name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0 &&
                        asset.Path.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    matches++;

                    bool isCurrent = string.Equals(asset.Path, current, StringComparison.OrdinalIgnoreCase);
                    bool picked;

                    if (useGrid)
                    {
                        if (column > 0)
                            ImGui.SameLine();

                        column = (column + 1) % columns;
                        picked = DrawGridItem(asset, name, isCurrent, workspace);
                    }
                    else
                    {
                        picked = ImGui.Selectable($"{name}##{asset.Path}", isCurrent);

                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(asset.Path);
                    }

                    if (picked)
                    {
                        result = JsonValue.Create(asset.Path);
                        ImGui.CloseCurrentPopup();
                    }
                }

                if (matches == 0)
                    ImGui.TextDisabled("(일치하는 에셋 없음)");
            }

            ImGui.EndChild();
            ImGui.EndPopup();

            return result;
        }

        private static bool DrawGridItem(AssetEntry asset, string name, bool isCurrent, EditorWorkspace workspace)
        {
            ImGui.PushID(asset.Path);
            ImGui.BeginGroup();

            bool clicked = ImGui.Button("##Thumb", new Vector2(ThumbSize, ThumbSize));

            var drawList = ImGui.GetWindowDrawList();
            var thumbMin = ImGui.GetItemRectMin();
            var thumb = LoadThumbnail(workspace, asset.Path);

            if (thumb != null && thumb.Width > 0 && thumb.Height > 0)
            {
                float scale = MathF.Min(ThumbSize / thumb.Width, ThumbSize / thumb.Height) * 0.92f;
                var size = new Vector2(thumb.Width, thumb.Height) * scale;
                var offset = (new Vector2(ThumbSize, ThumbSize) - size) * 0.5f;

                drawList.AddImage(new ImTextureRef(null, thumb.Handle), thumbMin + offset, thumbMin + offset + size);
            }
            else
            {
                var center = thumbMin + new Vector2(ThumbSize, ThumbSize) * 0.5f;
                var textSize = ImGui.CalcTextSize("?");
                drawList.AddText(center - textSize * 0.5f, ImGui.GetColorU32(ImGuiCol.TextDisabled), "?");
            }

            ImGui.Text(TruncateToWidth(name, ThumbSize));
            ImGui.EndGroup();

            if (isCurrent)
                drawList.AddRect(ImGui.GetItemRectMin() - Vector2.One, ImGui.GetItemRectMax() + Vector2.One,
                    ImGui.GetColorU32(ImGuiCol.CheckMark));

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(asset.Path);

            ImGui.PopID();
            return clicked;
        }

        private static ITexture LoadThumbnail(EditorWorkspace workspace, string relativePath)
        {
            if (workspace == null || string.IsNullOrEmpty(relativePath))
                return null;

            if (_brokenThumbnails.Contains(relativePath))
                return null;

            try
            {
                return AssetManager.Load<ITexture>(Path.Combine(workspace.RootPath, relativePath));
            }
            catch
            {
                _brokenThumbnails.Add(relativePath);
                return null;
            }
        }

        private static string TruncateToWidth(string text, float width)
        {
            if (ImGui.CalcTextSize(text).X <= width)
                return text;

            while (text.Length > 1 && ImGui.CalcTextSize(text + "..").X > width)
                text = text[..^1];

            return text + "..";
        }

        private static string TypeDisplayName(Type type)
        {
            string name = type.Name;

            if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
                name = name.Substring(1);

            return name;
        }
    }
}

using Hexa.NET.ImGui;
using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Rendering;
using IrisEditor.Workspace;
using System;
using System.Numerics;

namespace IrisEditor.Panels
{
    internal sealed class ScenePanel : EditorPanel
    {
        public bool ShowColliders = true;

        private readonly EditorContext _context;
        private readonly SceneRenderer _renderer;

        private Vector2 _viewPos = Vector2.Zero;
        private float _viewScale = 100f;

        private ActorData _dragActor;
        private Vector2 _dragOffset;
        private (int X, int Y)? _lastPaintCell;

        public ScenePanel(EditorContext context, SceneRenderer renderer)
        {
            _context = context;
            _renderer = renderer;
        }

        public override string Title => "씬";

        protected override void OnGui()
        {
            var draw = ImGui.GetWindowDrawList();
            var origin = ImGui.GetCursorScreenPos();
            var size = ImGui.GetContentRegionAvail();

            if (size.X < 1f || size.Y < 1f)
                return;

            var center = origin + size * 0.5f;

            if (!ImGui.GetDragDropPayload().IsNull)
            {
                ImGui.InvisibleButton("##SceneCanvas", size);

                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload(AssetDragDrop.PayloadType);

                    if (!payload.IsNull && AssetDragDrop.Current != null &&
                        AssetDragDrop.Current.AssetType == typeof(Prefab))
                    {
                        var world = SceneRenderer.PanelToWorld(ImGui.GetMousePos(), _viewPos, _viewScale, center);
                        _context.InstantiatePrefab(AssetDragDrop.Current.Path, world);
                    }

                    ImGui.EndDragDropTarget();
                }
            }

            HandleInput(center);

            draw.AddRectFilled(origin, origin + size, 0xFF202020);

            DrawCameraBackground(draw, center);
            _renderer.DrawSprites(draw, _viewPos, _viewScale, center, showSelection: true);

            if (ShowColliders)
                _renderer.DrawColliders(draw, _viewPos, _viewScale, center);

            DrawCameraMarker(draw, center);
            SceneRenderer.DrawGrid(draw, origin, size, _viewPos, _viewScale, center);
        }

        private void HandleInput(Vector2 center)
        {
            var io = ImGui.GetIO();

            if (ImGui.IsWindowHovered())
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle, 0f) || ImGui.IsMouseDragging(ImGuiMouseButton.Right, 0f))
                {
                    _viewPos.X -= io.MouseDelta.X / _viewScale;
                    _viewPos.Y += io.MouseDelta.Y / _viewScale;
                }

                if (io.MouseWheel != 0f)
                {
                    var mouse = ImGui.GetMousePos();
                    var worldUnderMouse = SceneRenderer.PanelToWorld(mouse, _viewPos, _viewScale, center);

                    _viewScale = Math.Clamp(_viewScale * MathF.Pow(1.1f, io.MouseWheel), 2f, 2000f);

                    _viewPos.X = worldUnderMouse.X - (mouse.X - center.X) / _viewScale;
                    _viewPos.Y = worldUnderMouse.Y + (mouse.Y - center.Y) / _viewScale;
                }

                if (HandleTilePaint(center))
                    return;

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    var mouse = ImGui.GetMousePos();
                    var picked = PickActor(mouse, center);
                    _context.Selected = picked;

                    if (picked != null)
                    {
                        var world = SceneRenderer.PanelToWorld(mouse, _viewPos, _viewScale, center);
                        _dragOffset = SceneTransforms.GetWorld(_context.Scene, picked).Position - world;
                        _dragActor = picked;
                    }
                }
            }

            if (_dragActor == null)
                return;

            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                _dragActor = null;
                return;
            }

            var dragTransform = _dragActor.GetComponent(typeof(Transform));
            if (dragTransform != null)
            {
                var world = SceneRenderer.PanelToWorld(ImGui.GetMousePos(), _viewPos, _viewScale, center);
                var newPosition = world + _dragOffset;

                if (newPosition != SceneTransforms.GetWorld(_context.Scene, _dragActor).Position)
                {
                    SceneTransforms.SetWorldPosition(_context.Scene, _dragActor, newPosition);
                    _context.MarkDirty();
                }
            }
        }

        private bool HandleTilePaint(Vector2 center)
        {
            if (!TileBrush.Active)
                return false;

            var actor = _context.Selected;
            var tilemap = actor?.GetComponent(typeof(Tilemap));

            if (tilemap == null)
                return false;

            var (origin, cellSize) = TilemapEdit.FindGrid(_context.Scene, actor);
            var mouseWorld = SceneRenderer.PanelToWorld(ImGui.GetMousePos(), _viewPos, _viewScale, center);
            var cell = TilemapEdit.WorldToCell(mouseWorld, origin, cellSize);

            DrawCellHighlight(origin, cellSize, cell, center);

            if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                if (_lastPaintCell != cell || ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    _lastPaintCell = cell;

                    bool changed = TileBrush.Eraser
                        ? TilemapEdit.Erase(tilemap, cell)
                        : TilemapEdit.Paint(tilemap, cell, TileBrush.TilePath);

                    if (changed)
                        _context.MarkDirty();
                }
            }
            else
            {
                _lastPaintCell = null;
            }

            return true;
        }

        private void DrawCellHighlight(Vector2 origin, Vector2 cellSize, (int X, int Y) cell, Vector2 center)
        {
            float sizeX = cellSize.X > 0f ? cellSize.X : 1f;
            float sizeY = cellSize.Y > 0f ? cellSize.Y : 1f;

            var worldMin = new Vector2(origin.X + cell.X * sizeX, origin.Y + cell.Y * sizeY);
            var worldMax = worldMin + new Vector2(sizeX, sizeY);

            var topLeft = SceneRenderer.WorldToPanel(new Vector2(worldMin.X, worldMax.Y), _viewPos, _viewScale, center);
            var bottomRight = SceneRenderer.WorldToPanel(new Vector2(worldMax.X, worldMin.Y), _viewPos, _viewScale, center);

            ImGui.GetWindowDrawList().AddRect(topLeft, bottomRight, 0xFF00A0FF);
        }

        private ActorData PickActor(Vector2 mouse, Vector2 center)
        {
            var actors = _context.Scene.Actors;

            for (int i = actors.Count - 1; i >= 0; i--)
            {
                var actor = actors[i];
                var transform = actor.GetComponent(typeof(Transform));
                if (transform == null)
                    continue;

                var (position, rotation, actorScale) = SceneTransforms.GetWorld(_context.Scene, actor);
                var screenPos = SceneRenderer.WorldToPanel(position, _viewPos, _viewScale, center);

                foreach (var comp in actor.Components)
                {
                    if (comp.TargetType != typeof(SpriteRenderer))
                        continue;

                    var sprite = _renderer.GetSprite(comp.GetString("Sprite", null));
                    var texture = sprite?.Texture;
                    if (texture == null)
                        continue;

                    float ppu = comp.GetFloat("PixelPerUnit", 100f);
                    if (ppu <= 0f)
                        continue;

                    var src = sprite?.SrcRect;
                    float pixelWidth = src.HasValue ? src.Value.Size.X : texture.Width;
                    float pixelHeight = src.HasValue ? src.Value.Size.Y : texture.Height;

                    float halfWidth = MathF.Abs(pixelWidth / ppu * actorScale.X) * _viewScale * 0.5f;
                    float halfHeight = MathF.Abs(pixelHeight / ppu * actorScale.Y) * _viewScale * 0.5f;

                    if (halfWidth <= 0f || halfHeight <= 0f)
                        continue;

                    var local = mouse - screenPos;

                    if (rotation != 0f)
                    {
                        float rad = rotation * MathF.PI / 180f;
                        float cos = MathF.Cos(rad);
                        float sin = MathF.Sin(rad);
                        local = new Vector2(local.X * cos + local.Y * sin, -local.X * sin + local.Y * cos);
                    }

                    if (MathF.Abs(local.X) <= halfWidth && MathF.Abs(local.Y) <= halfHeight)
                        return actor;
                }

                if (Vector2.Distance(mouse, screenPos) <= 8f)
                    return actor;
            }

            return null;
        }

        private void DrawCameraBackground(ImDrawListPtr draw, Vector2 center)
        {
            if (!_renderer.TryGetCamera(out var camWorld, out float camScale))
                return;

            if (camScale <= 0f)
                return;

            float halfWidth = _renderer.ReferenceWidth / camScale * 0.5f;
            float halfHeight = _renderer.ReferenceHeight / camScale * 0.5f;

            var min = SceneRenderer.WorldToPanel(camWorld + new Vector2(-halfWidth, halfHeight), _viewPos, _viewScale, center);
            var max = SceneRenderer.WorldToPanel(camWorld + new Vector2(halfWidth, -halfHeight), _viewPos, _viewScale, center);

            draw.AddRectFilled(min, max, _renderer.GetCameraBackground());
        }

        private void DrawCameraMarker(ImDrawListPtr draw, Vector2 center)
        {
            if (!_renderer.TryGetCamera(out var camWorld, out float camScale))
                return;

            if (camScale <= 0f)
                return;

            float halfWidth = _renderer.ReferenceWidth / camScale * 0.5f;
            float halfHeight = _renderer.ReferenceHeight / camScale * 0.5f;

            var min = SceneRenderer.WorldToPanel(camWorld + new Vector2(-halfWidth, halfHeight), _viewPos, _viewScale, center);
            var max = SceneRenderer.WorldToPanel(camWorld + new Vector2(halfWidth, -halfHeight), _viewPos, _viewScale, center);
            var p = SceneRenderer.WorldToPanel(camWorld, _viewPos, _viewScale, center);

            draw.AddRect(min, max, 0xFFFFFFFF);
            draw.AddCircle(p, 3f, 0xFFFFFFFF);
            draw.AddText(min + new Vector2(4f, 2f), 0xFFFFFFFF, "Cam");
        }
    }
}

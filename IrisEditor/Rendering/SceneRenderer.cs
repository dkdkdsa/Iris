using Hexa.NET.ImGui;
using Iris.Assets;
using Iris.Core;
using IrisEditor.Data;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json.Nodes;

namespace IrisEditor.Rendering
{
    internal sealed class SceneRenderer
    {
        private struct SpritePreview
        {
            public ITexture Texture;
            public Silk.NET.Maths.Rectangle<int>? Src;
            public Vector2 Center;
            public Vector2 Size;
            public float Rotation;
            public bool FlipX;
            public bool FlipY;
            public uint Tint;
            public int Order;
            public int Sequence;
            public bool Selected;
        }

        public float ReferenceWidth = 800f;
        public float ReferenceHeight = 600f;

        private readonly EditorContext _context;
        private readonly Dictionary<string, ITexture> _textures = new();
        private readonly Dictionary<string, SpriteAnimation> _clips = new();
        private readonly Dictionary<string, Tile> _tileAssets = new();
        private readonly List<SpritePreview> _previews = new();
        private EditorWorkspace _cacheWorkspace;

        public SceneRenderer(EditorContext context)
        {
            _context = context;
        }

        public bool TryGetCamera(out Vector2 position, out float scale)
        {
            foreach (var actor in _context.Scene.Actors)
            {
                var cam = actor.GetComponent(typeof(Camera));
                if (cam == null)
                    continue;

                position = SceneTransforms.GetWorld(_context.Scene, actor).Position;
                scale = cam.GetFloat("PixelPerUnit", 100f) * cam.GetFloat("Zoom", 1f);
                return true;
            }

            position = Vector2.Zero;
            scale = 100f;
            return false;
        }

        public void DrawSprites(ImDrawListPtr draw, Vector2 camPos, float scale, Vector2 center, bool showSelection)
        {
            _previews.Clear();

            foreach (var actor in _context.Scene.Actors)
            {
                var transform = actor.GetComponent(typeof(Transform));
                if (transform == null)
                    continue;

                var (position, rotation, actorScale) = SceneTransforms.GetWorld(_context.Scene, actor);
                bool selected = showSelection && _context.Selected == actor;

                foreach (var comp in actor.Components)
                {
                    if (comp.TargetType == typeof(TextureRenderer))
                    {
                        var texture = GetTexture(comp.GetString("Texture", null));

                        if (texture != null)
                            AddPreview(comp, texture, null, texture.Width, texture.Height,
                                position, actorScale, rotation, camPos, scale, center, selected);
                    }
                    else if (comp.TargetType == typeof(AnimatedSpriteRenderer))
                    {
                        var clip = GetClip(comp.GetString("Clip", null));

                        if (clip != null && clip.FrameCount > 0 && clip.Texture != null)
                        {
                            var frame = clip.GetFrame(0);
                            AddPreview(comp, clip.Texture, frame, frame.Size.X, frame.Size.Y,
                                position, actorScale, rotation, camPos, scale, center, selected);
                        }
                    }
                    else if (comp.TargetType == typeof(TilemapRenderer))
                    {
                        var tilemap = actor.GetComponent(typeof(Tilemap));

                        if (tilemap != null)
                            AddTilemapPreviews(actor, comp, tilemap, camPos, scale, center);
                    }
                }

                if (selected)
                {
                    var screenPos = WorldToPanel(position, camPos, scale, center);
                    draw.AddLine(screenPos - new Vector2(6f, 0f), screenPos + new Vector2(6f, 0f), 0xFF00A0FF);
                    draw.AddLine(screenPos - new Vector2(0f, 6f), screenPos + new Vector2(0f, 6f), 0xFF00A0FF);
                }
            }

            _previews.Sort(static (a, b) => a.Order != b.Order
                ? a.Order.CompareTo(b.Order)
                : a.Sequence.CompareTo(b.Sequence));

            foreach (var preview in _previews)
            {
                DrawSprite(draw, preview);

                if (preview.Selected)
                {
                    var half = preview.Size * 0.5f;
                    draw.AddRect(preview.Center - half, preview.Center + half, 0xFF00A0FF);
                }
            }
        }

        private void AddPreview(ComponentData comp, ITexture texture, Silk.NET.Maths.Rectangle<int>? src,
            float pixelWidth, float pixelHeight, Vector2 position, Vector2 actorScale, float rotation,
            Vector2 camPos, float scale, Vector2 center, bool selected)
        {
            float ppu = comp.GetFloat("PixelPerUnit", 100f);
            if (ppu <= 0f)
                return;

            var worldSize = new Vector2(pixelWidth / ppu * actorScale.X, pixelHeight / ppu * actorScale.Y);
            if (worldSize.X <= 0f || worldSize.Y <= 0f)
                return;

            var pivot = comp.GetVector2("Pivot", new Vector2(0.5f, 0.5f));
            var worldCenter = position + (new Vector2(0.5f, 0.5f) - pivot) * worldSize;

            _previews.Add(new SpritePreview
            {
                Texture = texture,
                Src = src,
                Center = WorldToPanel(worldCenter, camPos, scale, center),
                Size = worldSize * scale,
                Rotation = rotation,
                FlipX = comp.GetBool("FlipX", false),
                FlipY = comp.GetBool("FlipY", false),
                Tint = GetTint(comp),
                Order = (int)comp.GetFloat("Order", 0f),
                Sequence = _previews.Count,
                Selected = selected,
            });
        }

        private void AddTilemapPreviews(ActorData actor, ComponentData renderer, ComponentData tilemap,
            Vector2 camPos, float scale, Vector2 center)
        {
            var cells = TilemapEdit.GetCells(tilemap, out var palette);

            if (cells.Count == 0)
                return;

            var (origin, cellSize) = TilemapEdit.FindGrid(_context.Scene, actor);

            if (cellSize.X <= 0f || cellSize.Y <= 0f)
                return;

            uint tint = GetTint(renderer);
            int order = (int)renderer.GetFloat("Order", 0f);

            foreach (var ((x, y), index) in cells)
            {
                if (index < 0 || index >= palette.Count)
                    continue;

                var tile = GetTile(palette[index]);

                if (tile?.Texture == null)
                    continue;

                var worldCenter = new Vector2(
                    origin.X + (x + 0.5f) * cellSize.X,
                    origin.Y + (y + 0.5f) * cellSize.Y);

                _previews.Add(new SpritePreview
                {
                    Texture = tile.Texture,
                    Src = tile.SrcRect,
                    Center = WorldToPanel(worldCenter, camPos, scale, center),
                    Size = cellSize * scale,
                    Rotation = 0f,
                    Tint = tint,
                    Order = order,
                    Sequence = _previews.Count,
                });
            }
        }

        private static uint GetTint(ComponentData comp)
        {
            return GetColor(comp, "Color", 0xFFFFFFFF);
        }

        private static uint GetColor(ComponentData comp, string key, uint fallback)
        {
            if (comp.Properties is JsonObject obj && obj[key] is JsonArray { Count: 4 } arr &&
                arr[0] is JsonValue r && r.TryGetValue(out float rf) &&
                arr[1] is JsonValue g && g.TryGetValue(out float gf) &&
                arr[2] is JsonValue b && b.TryGetValue(out float bf) &&
                arr[3] is JsonValue a && a.TryGetValue(out float af))
            {
                uint rb = (uint)Math.Clamp((int)rf, 0, 255);
                uint gb = (uint)Math.Clamp((int)gf, 0, 255);
                uint bb = (uint)Math.Clamp((int)bf, 0, 255);
                uint ab = (uint)Math.Clamp((int)af, 0, 255);
                return ab << 24 | bb << 16 | gb << 8 | rb;
            }

            return fallback;
        }

        public uint GetCameraBackground()
        {
            foreach (var actor in _context.Scene.Actors)
            {
                var cam = actor.GetComponent(typeof(Camera));
                if (cam == null)
                    continue;

                return GetColor(cam, "BackgroundColor", 0xFF000000) | 0xFF000000;
            }

            return 0xFF000000;
        }

        public static void DrawGrid(ImDrawListPtr draw, Vector2 origin, Vector2 size, Vector2 cam, float scale, Vector2 center)
        {
            if (scale < 4f)
                return;

            const uint color = 0x80808080;

            float worldLeft = (origin.X - center.X) / scale + cam.X;
            float worldRight = (origin.X + size.X - center.X) / scale + cam.X;
            float worldTop = cam.Y - (origin.Y - center.Y) / scale;
            float worldBottom = cam.Y - (origin.Y + size.Y - center.Y) / scale;

            for (float x = MathF.Ceiling(worldLeft); x <= worldRight; x += 1f)
            {
                float screenX = (x - cam.X) * scale + center.X;
                draw.AddLine(new Vector2(screenX, origin.Y), new Vector2(screenX, origin.Y + size.Y), color);
            }

            for (float y = MathF.Ceiling(worldBottom); y <= worldTop; y += 1f)
            {
                float screenY = (cam.Y - y) * scale + center.Y;
                draw.AddLine(new Vector2(origin.X, screenY), new Vector2(origin.X + size.X, screenY), color);
            }
        }

        public static Vector2 WorldToPanel(Vector2 world, Vector2 cam, float scale, Vector2 center)
        {
            return new Vector2(
                (world.X - cam.X) * scale + center.X,
                (cam.Y - world.Y) * scale + center.Y);
        }

        public static Vector2 PanelToWorld(Vector2 panel, Vector2 cam, float scale, Vector2 center)
        {
            return new Vector2(
                (panel.X - center.X) / scale + cam.X,
                cam.Y - (panel.Y - center.Y) / scale);
        }

        private static unsafe void DrawSprite(ImDrawListPtr draw, in SpritePreview preview)
        {
            var half = preview.Size * 0.5f;
            var texRef = new ImTextureRef(null, preview.Texture.Handle);

            var uv0 = Vector2.Zero;
            var uv1 = Vector2.One;

            if (preview.Src.HasValue)
            {
                var src = preview.Src.Value;
                float w = preview.Texture.Width;
                float h = preview.Texture.Height;
                uv0 = new Vector2(src.Origin.X / w, src.Origin.Y / h);
                uv1 = new Vector2((src.Origin.X + src.Size.X) / w, (src.Origin.Y + src.Size.Y) / h);
            }

            if (preview.FlipX)
                (uv0.X, uv1.X) = (uv1.X, uv0.X);

            if (preview.FlipY)
                (uv0.Y, uv1.Y) = (uv1.Y, uv0.Y);

            if (preview.Rotation == 0f)
            {
                draw.AddImage(texRef, preview.Center - half, preview.Center + half, uv0, uv1, preview.Tint);
                return;
            }

            float rad = preview.Rotation * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);
            var c = preview.Center;

            Vector2 Corner(float x, float y) => c + new Vector2(x * cos - y * sin, x * sin + y * cos);

            draw.AddImageQuad(texRef,
                Corner(-half.X, -half.Y), Corner(half.X, -half.Y),
                Corner(half.X, half.Y), Corner(-half.X, half.Y),
                new Vector2(uv0.X, uv0.Y), new Vector2(uv1.X, uv0.Y),
                new Vector2(uv1.X, uv1.Y), new Vector2(uv0.X, uv1.Y),
                preview.Tint);
        }

        public ITexture GetTexture(string path)
        {
            EnsureCache();

            if (string.IsNullOrWhiteSpace(path))
                return null;

            if (_textures.TryGetValue(path, out var cached))
                return cached;

            ITexture texture = null;

            try
            {
                string fullPath = ResolvePath(path);
                if (File.Exists(fullPath))
                    texture = AssetManager.Load<ITexture>(fullPath);
            }
            catch
            {
            }

            _textures[path] = texture;
            return texture;
        }

        public SpriteAnimation GetClip(string path)
        {
            EnsureCache();

            if (string.IsNullOrWhiteSpace(path))
                return null;

            if (_clips.TryGetValue(path, out var cached))
                return cached;

            SpriteAnimation clip = null;

            try
            {
                string fullPath = ResolvePath(path);
                if (File.Exists(fullPath))
                    clip = AssetManager.Load<SpriteAnimation>(fullPath);
            }
            catch
            {
            }

            _clips[path] = clip;
            return clip;
        }

        public Tile GetTile(string path)
        {
            EnsureCache();

            if (string.IsNullOrWhiteSpace(path))
                return null;

            if (_tileAssets.TryGetValue(path, out var cached))
                return cached;

            Tile tile = null;

            try
            {
                string fullPath = ResolvePath(path);
                if (File.Exists(fullPath))
                    tile = AssetManager.Load<Tile>(fullPath);
            }
            catch
            {
            }

            _tileAssets[path] = tile;
            return tile;
        }

        private void EnsureCache()
        {
            if (_cacheWorkspace == _context.Workspace)
                return;

            _textures.Clear();
            _clips.Clear();
            _tileAssets.Clear();
            _cacheWorkspace = _context.Workspace;
        }

        private string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);

            return _context.Workspace != null ? _context.Workspace.ToAbsolute(path) : Path.GetFullPath(path);
        }
    }
}

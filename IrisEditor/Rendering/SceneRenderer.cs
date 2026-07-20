using Hexa.NET.ImGui;
using Iris.Assets;
using Iris.Core;
using IrisEditor.Workspace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace IrisEditor.Rendering
{
    internal sealed class SceneRenderer
    {
        public float ReferenceWidth = 800f;
        public float ReferenceHeight = 600f;

        private readonly EditorContext _context;
        private readonly Dictionary<string, ITexture> _textures = new();
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

                var transform = actor.GetComponent(typeof(Transform));
                position = transform?.GetVector2("Position", Vector2.Zero) ?? Vector2.Zero;
                scale = cam.GetFloat("PixelPerUnit", 100f) * cam.GetFloat("Zoom", 1f);
                return true;
            }

            position = Vector2.Zero;
            scale = 100f;
            return false;
        }

        public void DrawSprites(ImDrawListPtr draw, Vector2 camPos, float scale, Vector2 center, bool showSelection)
        {
            foreach (var actor in _context.Scene.Actors)
            {
                var transform = actor.GetComponent(typeof(Transform));
                if (transform == null)
                    continue;

                var position = transform.GetVector2("Position", Vector2.Zero);
                var actorScale = transform.GetVector2("Scale", Vector2.One);
                float rotation = transform.GetFloat("Rotation", 0f);

                var screenPos = WorldToPanel(position, camPos, scale, center);
                bool selected = showSelection && _context.Selected == actor;

                foreach (var comp in actor.Components)
                {
                    if (comp.TargetType != typeof(TextureRenderer))
                        continue;

                    var texture = GetTexture(comp.GetString("Texture", null));
                    if (texture == null)
                        continue;

                    float ppu = comp.GetFloat("PixelPerUnit", 100f);
                    if (ppu <= 0f)
                        continue;

                    float width = texture.Width / ppu * actorScale.X * scale;
                    float height = texture.Height / ppu * actorScale.Y * scale;

                    if (width <= 0f || height <= 0f)
                        continue;

                    DrawSprite(draw, texture, screenPos, width, height, rotation);

                    if (selected)
                    {
                        var half = new Vector2(width, height) * 0.5f;
                        draw.AddRect(screenPos - half, screenPos + half, 0xFF00A0FF);
                    }
                }

                if (selected)
                {
                    draw.AddLine(screenPos - new Vector2(6f, 0f), screenPos + new Vector2(6f, 0f), 0xFF00A0FF);
                    draw.AddLine(screenPos - new Vector2(0f, 6f), screenPos + new Vector2(0f, 6f), 0xFF00A0FF);
                }
            }
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

        private static unsafe void DrawSprite(ImDrawListPtr draw, ITexture texture, Vector2 center, float width, float height, float rotationDeg)
        {
            var half = new Vector2(width, height) * 0.5f;
            var texRef = new ImTextureRef(null, texture.Handle);

            if (rotationDeg == 0f)
            {
                draw.AddImage(texRef, center - half, center + half);
                return;
            }

            float rad = rotationDeg * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            Vector2 Corner(float x, float y) => center + new Vector2(x * cos - y * sin, x * sin + y * cos);

            draw.AddImageQuad(texRef,
                Corner(-half.X, -half.Y), Corner(half.X, -half.Y),
                Corner(half.X, half.Y), Corner(-half.X, half.Y),
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f),
                0xFFFFFFFF);
        }

        public ITexture GetTexture(string path)
        {
            if (_cacheWorkspace != _context.Workspace)
            {
                _textures.Clear();
                _cacheWorkspace = _context.Workspace;
            }

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

        private string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);

            return _context.Workspace != null ? _context.Workspace.ToAbsolute(path) : Path.GetFullPath(path);
        }
    }
}

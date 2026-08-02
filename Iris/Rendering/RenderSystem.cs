using Iris.Core;
using Iris.Debugging;
using Iris.Diagnostics;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using Debug = Iris.Debugging.Debug;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Iris.Rendering
{
    public class RenderSystem : SystemBase
    {
        private static readonly Comparison<RenderCommand> _bySubmission = static (a, b) => a.order != b.order
            ? a.order.CompareTo(b.order)
            : a.sequence.CompareTo(b.sequence);

        private static readonly Comparison<RenderCommand> _byTexture = static (a, b) =>
        {
            if (a.order != b.order)
                return a.order.CompareTo(b.order);

            nint left = a.texture?.Handle ?? 0;
            nint right = b.texture?.Handle ?? 0;

            return left != right ? left.CompareTo(right) : a.sequence.CompareTo(b.sequence);
        };

        private List<RenderCommand> _commands = new();
        private IRenderBackend _backend;

        public Vector2D<int> Viewport { get; internal set; }

        public bool CullingEnabled { get; set; } = true;

        public bool SortByTexture { get; set; }

        internal RenderSystem(IRenderBackend backend, int viewportWidth, int viewportHeight)
        {
            _backend = backend;
            Viewport = new Vector2D<int>(viewportWidth, viewportHeight);
        }

        internal void Flush()
        {
            bool measure = Stats.Enabled;
            long started = measure ? Stopwatch.GetTimestamp() : 0L;
            int submitted = _commands.Count;

            var camera = Camera.Main;

            if (camera != null)
                camera.SetViewport(Viewport);
            else if (submitted > 0)
                Debug.LogOnce(LogLevel.Warning, "No active camera; world-space draws are skipped.");

            var bounds = CullingEnabled && camera != null ? camera.WorldBounds : default;
            bool cull = bounds.Size.X > 0f && bounds.Size.Y > 0f;

            int kept = 0;

            for (int i = 0; i < _commands.Count; i++)
            {
                var cmd = _commands[i];

                if (!cmd.screenSpace)
                {
                    if (camera == null)
                        continue;

                    if (cull && !Intersects(cmd.dest, cmd.rotation, bounds))
                        continue;
                }

                _commands[kept++] = cmd;
            }

            _commands.RemoveRange(kept, _commands.Count - kept);
            _commands.Sort(SortByTexture ? _byTexture : _bySubmission);

            int switches = 0;
            nint bound = 0;

            foreach (var cmd in _commands)
            {
                nint handle = cmd.texture?.Handle ?? 0;

                if (handle != bound)
                {
                    switches++;
                    bound = handle;
                }

                if (cmd.screenSpace)
                {
                    var d = cmd.dest;
                    var dest = new Rectangle<int>(
                        (int)MathF.Round(d.Origin.X), (int)MathF.Round(d.Origin.Y),
                        (int)MathF.Round(d.Size.X), (int)MathF.Round(d.Size.Y));

                    _backend.DrawTexture(cmd.texture, cmd.src, dest, cmd.rotation, cmd.flipX, cmd.flipY, cmd.color);
                }
                else
                {
                    _backend.DrawTexture(
                        cmd.texture, cmd.src, camera.WorldToScreen(cmd.dest),
                        cmd.rotation, cmd.flipX, cmd.flipY, cmd.color);
                }
            }

            _commands.Clear();

            if (measure)
                Stats.RecordRender(submitted, kept, switches, Stopwatch.GetElapsedTime(started));
        }

        internal static bool Intersects(in Rectangle<float> dest, float rotation, in Rectangle<float> bounds)
        {
            float minX = dest.Origin.X;
            float minY = dest.Origin.Y;
            float maxX = minX + dest.Size.X;
            float maxY = minY + dest.Size.Y;

            if (rotation != 0f)
            {
                float halfWidth = dest.Size.X * 0.5f;
                float halfHeight = dest.Size.Y * 0.5f;
                float radius = MathF.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight);

                float centerX = minX + halfWidth;
                float centerY = minY + halfHeight;

                minX = centerX - radius;
                maxX = centerX + radius;
                minY = centerY - radius;
                maxY = centerY + radius;
            }

            return maxX >= bounds.Origin.X
                && minX <= bounds.Origin.X + bounds.Size.X
                && maxY >= bounds.Origin.Y
                && minY <= bounds.Origin.Y + bounds.Size.Y;
        }

        public void Submit(in RenderCommand cmd)
        {
            var entry = cmd;
            entry.sequence = _commands.Count;

            _commands.Add(entry);
        }
    }
}

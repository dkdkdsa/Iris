using Iris.Core;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Iris.Rendering
{
    public class RenderSystem : SystemBase
    {
        private List<RenderCommand> _commands = new();
        private IRenderBackend _backend;
        private bool _warnedNoCamera;

        public Vector2D<int> Viewport { get; internal set; }

        internal RenderSystem(IRenderBackend backend, int viewportWidth, int viewportHeight)
        {
            _backend = backend;
            Viewport = new Vector2D<int>(viewportWidth, viewportHeight);
        }

        internal void Flush()
        {
            var camera = Camera.Main;

            if (camera != null)
                camera.SetViewport(Viewport);
            else if (!_warnedNoCamera)
                _warnedNoCamera = true;

            _commands.Sort(static (a, b) => a.order != b.order
                ? a.order.CompareTo(b.order)
                : a.sequence.CompareTo(b.sequence));

            foreach (var cmd in _commands)
            {
                if (cmd.screenSpace)
                {
                    var d = cmd.dest;
                    var dest = new Rectangle<int>(
                        (int)MathF.Round(d.Origin.X), (int)MathF.Round(d.Origin.Y),
                        (int)MathF.Round(d.Size.X), (int)MathF.Round(d.Size.Y));

                    _backend.DrawTexture(cmd.texture, cmd.src, dest, cmd.rotation, cmd.flipX, cmd.flipY, cmd.color);
                }
                else if (camera != null)
                {
                    _backend.DrawTexture(
                        cmd.texture, cmd.src, camera.WorldToScreen(cmd.dest),
                        cmd.rotation, cmd.flipX, cmd.flipY, cmd.color);
                }
            }

            _commands.Clear();
        }

        public void Submit(in RenderCommand cmd)
        {
            var entry = cmd;
            entry.sequence = _commands.Count;

            _commands.Add(entry);
        }
    }
}

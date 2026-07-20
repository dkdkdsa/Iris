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
            {
                camera.SetViewport(Viewport);

                _commands.Sort((a, b) => a.order.CompareTo(b.order));

                foreach (var cmd in _commands)
                {
                    _backend.DrawTexture(
                        cmd.texture, cmd.src, camera.WorldToScreen(cmd.dest),
                        cmd.rotation, cmd.flipX, cmd.flipY);
                }
            }
            else if (!_warnedNoCamera)
            {
                _warnedNoCamera = true;
            }

            _commands.Clear();
        }

        public void Submit(in RenderCommand cmd) => _commands.Add(cmd);
    }
}

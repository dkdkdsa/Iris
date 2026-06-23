using Iris.Core;
using System;
using System.Collections.Generic;

namespace Iris.Rendering
{
    public class RenderSystem : SystemBase
    {
        private List<RenderCommand> _commands = new();
        private IRenderBackend _backend;

        public Camera Camera { get; set; }

        internal RenderSystem(IRenderBackend backend, int viewportWidth, int viewportHeight)
        {
            Order = int.MaxValue;
            _backend = backend;
            Camera = new Camera(viewportWidth, viewportHeight);
        }

        public override void LateUpdate()
        {
            _backend.BeginFrame();
            _backend.Clear();

            _commands.Sort((a, b) => a.order.CompareTo(b.order));

            

            foreach (var cmd in _commands)
            {
                _backend.DrawTexture(
                    cmd.texture, Camera.WorldToScreen(cmd.dest),
                    cmd.rotation, cmd.flipX, cmd.flipY);
            }


            _commands.Clear();
            _backend.EndFrame();
        }

        public void Submit(in RenderCommand cmd) => _commands.Add(cmd);
    }
}
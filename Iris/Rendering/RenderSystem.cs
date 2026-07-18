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

        /// <summary>
        /// 카메라가 그려 넣는 대상의 크기. Engine이 매 프레임 백버퍼 크기로 넣어준다.
        /// </summary>
        public Vector2D<int> Viewport { get; internal set; }

        internal RenderSystem(IRenderBackend backend, int viewportWidth, int viewportHeight)
        {
            _backend = backend;
            Viewport = new Vector2D<int>(viewportWidth, viewportHeight);
        }

        /// <summary>
        /// 쌓인 커맨드를 현재 렌더 타겟에 그린다.
        /// 프레임 경계(BeginFrame/Clear/present)는 AppHost가 갖고 있으므로 여기서는 그리기만 한다.
        /// </summary>
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
                Console.WriteLine("[Iris] 카메라가 없어서 아무것도 그리지 않는다. Actor에 Camera 컴포넌트를 붙여라.");
            }

            _commands.Clear();
        }

        public void Submit(in RenderCommand cmd) => _commands.Add(cmd);
    }
}

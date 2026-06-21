using Iris.Core;
using System.Collections.Generic;

namespace Iris.Rendering
{
    internal unsafe class RenderSystem : SystemBase
    {
        private List<IRenderObject> _renderObjects = new List<IRenderObject>();
        private IRenderBackend _backend;

        internal RenderSystem(IRenderBackend backend)
        {
            Order = 999999999;
            _backend = backend;
        }

        public override void Update()
        {
            _backend.BeginFrame();
            _backend.Clear();

            foreach (var ro in _renderObjects)
            {
                ro.Render(_backend);
            }

            _backend.EndFrame();
        }

        public void AddRenderObject(IRenderObject renderObject)
        {
            _renderObjects.Add(renderObject);
            renderObject.Init(_backend);
            _renderObjects.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public override void Dispose()
        {
            foreach (var ro in _renderObjects)
            {
                ro.Dispose();
            }
        }
    }
}
using Iris.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public abstract class RendererBase : Component
    {
        protected RenderSystem system;

        protected override void OnAttached()
        {
            system = SystemManager.Instance.GetSystem<RenderSystem>();
        }

        public override void LateUpdate()
        {
            Render();   
        }

        protected abstract void Render();

    }
}

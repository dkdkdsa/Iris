using Iris.Core;
using Iris.Rendering;

namespace Iris.UI
{
    public sealed class Canvas : Component
    {
        private RenderSystem _system;

        protected override void OnAttached()
        {
            _system = SystemManager.Instance.GetSystem<RenderSystem>();    
        }

        public override void LateUpdate()
        {
        }
    }
}

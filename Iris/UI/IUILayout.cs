using Iris.Assets;
using System.Collections.Generic;

namespace Iris.UI
{
    public interface IUILayout : IAsset
    {
        public IReadOnlyList<UIObject> Instantiate();
    }
}

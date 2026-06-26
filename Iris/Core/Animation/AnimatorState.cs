using System.Collections.Generic;

namespace Iris.Core
{
    internal sealed class AnimatorState
    {
        public string Name { get; }
        public SpriteAnimation Clip { get; }
        public List<AnimatorTransition> Transitions { get; } = new();

        public AnimatorState(string name, SpriteAnimation clip)
        {
            Name = name;
            Clip = clip;
        }
    }
}

using System.Collections.Generic;

namespace Iris.Core
{
    internal sealed class AnimatorState
    {
        public string Name { get; }
        public AnimationClip Clip { get; }
        public List<AnimatorTransition> Transitions { get; } = new();

        public AnimatorState(string name, AnimationClip clip)
        {
            Name = name;
            Clip = clip;
        }
    }
}

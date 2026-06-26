using System;

namespace Iris.Core
{
    internal sealed class AnimatorTransition
    {
        public AnimatorState To { get; set; }
        public Func<Animator, bool> Condition { get; set; }
        public bool HasExitTime { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Iris.Core
{
    internal sealed class AnimatorTransition
    {
        public AnimatorState To { get; set; }
        public Func<Animator, bool> Condition { get; set; }
        public List<AnimatorCondition> Conditions { get; } = new();
        public bool HasExitTime { get; set; }
    }
}

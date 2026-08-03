using Iris.Assets;
using System.Collections.Generic;

namespace Iris.Core
{
    public sealed class AnimatorParameter
    {
        public string Name { get; init; }
        public AnimatorParameterType Type { get; init; }
    }

    public sealed class AnimatorControllerTransition
    {
        public string To { get; init; }
        public bool HasExitTime { get; init; }
        public IReadOnlyList<AnimatorCondition> Conditions { get; init; } = new List<AnimatorCondition>();
    }

    public sealed class AnimatorControllerState
    {
        public string Name { get; init; }
        public AnimationClip Clip { get; init; }
        public IReadOnlyList<AnimatorControllerTransition> Transitions { get; init; } = new List<AnimatorControllerTransition>();
    }

    public sealed class AnimatorController : IAsset
    {
        public string DefaultState { get; init; }
        public IReadOnlyList<AnimatorParameter> Parameters { get; init; } = new List<AnimatorParameter>();
        public IReadOnlyList<AnimatorControllerState> States { get; init; } = new List<AnimatorControllerState>();
        public IReadOnlyList<AnimatorControllerTransition> AnyTransitions { get; init; } = new List<AnimatorControllerTransition>();

        public void Dispose()
        {
        }
    }
}

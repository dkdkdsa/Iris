using Iris.Core;
using System.Collections.Generic;
using System.Numerics;

namespace IrisEditor.Data
{
    internal sealed class AnimatorParameterData
    {
        public string Name = string.Empty;
        public AnimatorParameterType Type = AnimatorParameterType.Bool;
    }

    internal sealed class AnimatorConditionData
    {
        public string Parameter = string.Empty;
        public AnimatorConditionMode Mode = AnimatorConditionMode.If;
        public float Threshold;
    }

    internal sealed class AnimatorTransitionData
    {
        public string To = string.Empty;
        public bool HasExitTime;
        public List<AnimatorConditionData> Conditions = new();
    }

    internal sealed class AnimatorStateData
    {
        public string Name = string.Empty;
        public string Clip = string.Empty;
        public Vector2 Position;
        public List<AnimatorTransitionData> Transitions = new();
    }

    internal sealed class AnimatorGraph
    {
        public string DefaultState = string.Empty;
        public List<AnimatorParameterData> Parameters = new();
        public List<AnimatorStateData> States = new();
        public List<AnimatorTransitionData> AnyTransitions = new();

        public AnimatorStateData Find(string name)
        {
            return States.Find(x => x.Name == name);
        }

        public string UniqueStateName(string baseName)
        {
            if (Find(baseName) == null)
                return baseName;

            int index = 1;

            while (Find($"{baseName} ({index})") != null)
                index++;

            return $"{baseName} ({index})";
        }

        public string UniqueParameterName(string baseName)
        {
            if (Parameters.Find(x => x.Name == baseName) == null)
                return baseName;

            int index = 1;

            while (Parameters.Find(x => x.Name == $"{baseName} ({index})") != null)
                index++;

            return $"{baseName} ({index})";
        }
    }
}

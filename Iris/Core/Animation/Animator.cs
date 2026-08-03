using Iris.Attributes;
using Iris.Debugging;
using System;
using System.Collections.Generic;

namespace Iris.Core
{
    public sealed class Animator : Component
    {
        private const float Epsilon = 0.0001f;

        private readonly Dictionary<string, AnimatorState> _states = new();
        private readonly List<AnimatorTransition> _anyTransitions = new();

        private readonly Dictionary<string, bool> _bools = new();
        private readonly Dictionary<string, float> _floats = new();
        private readonly Dictionary<string, int> _ints = new();
        private readonly HashSet<string> _triggers = new();

        private AnimatorState _current;
        private bool _started;

        public AnimationPlayer Renderer { get; private set; }

        [Show]
        public AnimatorController Controller { get; set; }

        public string CurrentName => _current?.Name;

        protected override void Awake()
        {
            Renderer = GetComponent<AnimationPlayer>()
                       ?? OwnerActor.AddComponent<AnimationPlayer>();

            Build(Controller);
        }

        public void Build(AnimatorController controller)
        {
            _states.Clear();
            _anyTransitions.Clear();
            _current = null;
            _started = false;

            if (controller == null)
                return;

            foreach (var parameter in controller.Parameters)
            {
                switch (parameter.Type)
                {
                    case AnimatorParameterType.Bool: _bools[parameter.Name] = false; break;
                    case AnimatorParameterType.Float: _floats[parameter.Name] = 0f; break;
                    case AnimatorParameterType.Int: _ints[parameter.Name] = 0; break;
                }
            }

            foreach (var state in controller.States)
                _states[state.Name] = new AnimatorState(state.Name, state.Clip);

            foreach (var state in controller.States)
            {
                if (!_states.TryGetValue(state.Name, out var from))
                    continue;

                foreach (var transition in state.Transitions)
                {
                    var built = BuildTransition(transition);

                    if (built != null)
                        from.Transitions.Add(built);
                }
            }

            foreach (var transition in controller.AnyTransitions)
            {
                var built = BuildTransition(transition);

                if (built != null)
                    _anyTransitions.Add(built);
            }

            if (controller.DefaultState != null && _states.TryGetValue(controller.DefaultState, out var start))
                _current = start;
            else if (controller.States.Count > 0 && _states.TryGetValue(controller.States[0].Name, out var first))
                _current = first;
        }

        private AnimatorTransition BuildTransition(AnimatorControllerTransition source)
        {
            if (!_states.TryGetValue(source.To, out var target))
            {
                Debug.LogOnce(LogLevel.Warning, $"Animator transition target state not found: {source.To}", this);
                return null;
            }

            var result = new AnimatorTransition
            {
                To = target,
                HasExitTime = source.HasExitTime,
            };

            result.Conditions.AddRange(source.Conditions);
            return result;
        }

        public void AddState(string name, AnimationClip clip)
        {
            var state = new AnimatorState(name, clip);
            _states[name] = state;
            _current ??= state;
        }

        public void AddTransition(string from, string to, Func<Animator, bool> condition = null, bool hasExitTime = false)
        {
            _states[from].Transitions.Add(new AnimatorTransition
            {
                To = _states[to],
                Condition = condition,
                HasExitTime = hasExitTime,
            });
        }

        public void AddAnyTransition(string to, Func<Animator, bool> condition = null, bool hasExitTime = false)
        {
            _anyTransitions.Add(new AnimatorTransition
            {
                To = _states[to],
                Condition = condition,
                HasExitTime = hasExitTime,
            });
        }


        public void SetBool(string name, bool value) => _bools[name] = value;
        public bool GetBool(string name) => _bools.TryGetValue(name, out var v) && v;

        public void SetFloat(string name, float value) => _floats[name] = value;
        public float GetFloat(string name) => _floats.TryGetValue(name, out var v) ? v : 0f;

        public void SetInt(string name, int value) => _ints[name] = value;
        public int GetInt(string name) => _ints.TryGetValue(name, out var v) ? v : 0;

        public void SetTrigger(string name) => _triggers.Add(name);
        public bool GetTrigger(string name) => _triggers.Contains(name);

        public void Play(string name)
        {
            if (_states.TryGetValue(name, out var state))
                Enter(state);
        }

        private void Enter(AnimatorState state)
        {
            _current = state;
            _started = true;
            Renderer.Play(state.Clip);
        }

        public override void Update()
        {
            if (_current == null)
                return;

            if (!_started)
                Enter(_current);

            var next = Evaluate();
            if (next != null && next != _current)
                Enter(next);

            _triggers.Clear();
        }

        private AnimatorState Evaluate()
        {
            foreach (var t in _anyTransitions)
            {
                if (t.To == _current) 
                    continue;

                if (Passes(t))
                    return t.To;
            }

            foreach (var t in _current.Transitions)
            {
                if (Passes(t)) 
                    return t.To;
            }

            return null;
        }

        private bool Passes(AnimatorTransition t)
        {
            if (t.HasExitTime && Renderer.IsPlaying)
                return false;

            if (t.Condition != null && !t.Condition(this))
                return false;

            foreach (var condition in t.Conditions)
            {
                if (!Passes(condition))
                    return false;
            }

            return true;
        }

        private bool Passes(AnimatorCondition condition)
        {
            if (string.IsNullOrEmpty(condition.Parameter))
                return true;

            switch (condition.Mode)
            {
                case AnimatorConditionMode.If:
                    return IsSet(condition.Parameter);

                case AnimatorConditionMode.IfNot:
                    return !IsSet(condition.Parameter);

                case AnimatorConditionMode.Greater:
                    return Number(condition.Parameter) > condition.Threshold;

                case AnimatorConditionMode.Less:
                    return Number(condition.Parameter) < condition.Threshold;

                case AnimatorConditionMode.Equals:
                    return MathF.Abs(Number(condition.Parameter) - condition.Threshold) <= Epsilon;

                case AnimatorConditionMode.NotEquals:
                    return MathF.Abs(Number(condition.Parameter) - condition.Threshold) > Epsilon;
            }

            return true;
        }

        private bool IsSet(string parameter)
        {
            return GetBool(parameter) || GetTrigger(parameter);
        }

        private float Number(string parameter)
        {
            if (_floats.TryGetValue(parameter, out var f))
                return f;

            return _ints.TryGetValue(parameter, out var i) ? i : 0f;
        }
    }
}

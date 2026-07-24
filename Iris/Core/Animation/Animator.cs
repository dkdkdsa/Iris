using System;
using System.Collections.Generic;

namespace Iris.Core
{
    public sealed class Animator : Component
    {
        private readonly Dictionary<string, AnimatorState> _states = new();
        private readonly List<AnimatorTransition> _anyTransitions = new();

        private readonly Dictionary<string, bool> _bools = new();
        private readonly Dictionary<string, float> _floats = new();
        private readonly Dictionary<string, int> _ints = new();
        private readonly HashSet<string> _triggers = new();

        private AnimatorState _current;
        private bool _started;

        public AnimatedSpriteRenderer Renderer { get; private set; }

        public string CurrentName => _current?.Name;

        protected override void Awake()
        {
            Renderer = GetComponent<AnimatedSpriteRenderer>()
                       ?? OwnerActor.AddComponent<AnimatedSpriteRenderer>();
        }

        public void AddState(string name, SpriteAnimation clip)
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

            return t.Condition == null || t.Condition(this);
        }
    }
}

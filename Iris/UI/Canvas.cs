using Iris.Core;
using Iris.Rendering;
using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Iris.UI
{
    public sealed class Canvas : Component
    {
        public const int OrderBase = 1_000_000;

        private static readonly List<Canvas> _canvases = new();

        public static IReadOnlyList<Canvas> All => _canvases;

        private RenderSystem _system;
        private readonly List<UIObject> _uiObjects = new();
        private readonly List<RenderCommand> _buffer = new();

        public Vector2D<int> ReferenceSize { get; set; } = new(1280, 720);

        public float Match { get; set; } = 0.5f;

        public IUILayout Layout { get; set; }

        public IReadOnlyList<UIObject> UIObjects => _uiObjects;

        public UIObject Find(string name)
        {
            foreach (var uiObject in _uiObjects)
            {
                if (uiObject.Name == name)
                    return uiObject;
            }

            return null;
        }

        public T Find<T>(string name) where T : UIObject
        {
            foreach (var uiObject in _uiObjects)
            {
                if (uiObject is T typed && uiObject.Name == name)
                    return typed;
            }

            return null;
        }

        public void ApplyLayout()
        {
            foreach (var uiObject in _uiObjects)
                uiObject.Dispose();

            _uiObjects.Clear();

            if (Layout == null)
                return;

            foreach (var uiObject in Layout.Instantiate())
                _uiObjects.Add(uiObject);
        }

        public float UIScale
        {
            get
            {
                if (_system == null || ReferenceSize.X <= 0 || ReferenceSize.Y <= 0)
                    return 1f;

                float scaleX = _system.Viewport.X / (float)ReferenceSize.X;
                float scaleY = _system.Viewport.Y / (float)ReferenceSize.Y;

                return scaleX + (scaleY - scaleX) * Math.Clamp(Match, 0f, 1f);
            }
        }

        public void AddUIObject(UIObject uiObject)
        {
            if (uiObject == null || _uiObjects.Contains(uiObject))
                return;

            _uiObjects.Add(uiObject);
        }

        public void RemoveUIObject(UIObject uiObject)
        {
            _uiObjects.Remove(uiObject);
        }

        public Rectangle<float> CalculateRect(UIObject uiObject)
        {
            float scale = UIScale;
            var viewport = _system.Viewport;

            var size = uiObject.GetSize() * scale;
            var anchorPoint = new Vector2D<float>(viewport.X * uiObject.Anchor.X, viewport.Y * uiObject.Anchor.Y);
            var offset = uiObject.Position * scale;

            float x = anchorPoint.X + offset.X - size.X * uiObject.Anchor.X;
            float y = anchorPoint.Y + offset.Y - size.Y * uiObject.Anchor.Y;

            return new Rectangle<float>(x, y, size.X, size.Y);
        }

        public UIObject HitTest(Vector2D<float> screenPosition)
        {
            if (_system == null)
                return null;

            UIObject best = null;
            int bestOrder = int.MinValue;

            foreach (var uiObject in _uiObjects)
            {
                if (!uiObject.Visible)
                    continue;

                var rect = CalculateRect(uiObject);

                if (screenPosition.X < rect.Origin.X || screenPosition.X > rect.Origin.X + rect.Size.X ||
                    screenPosition.Y < rect.Origin.Y || screenPosition.Y > rect.Origin.Y + rect.Size.Y)
                    continue;

                if (uiObject.Order >= bestOrder)
                {
                    best = uiObject;
                    bestOrder = uiObject.Order;
                }
            }

            return best;
        }

        protected override void OnAttached()
        {
            _system = SystemManager.Instance.GetSystem<RenderSystem>();
            _canvases.Add(this);

            if (Layout != null)
                ApplyLayout();
        }

        public override void LateUpdate()
        {
            if (_system == null)
                return;

            _buffer.Clear();

            foreach (var uiObject in _uiObjects)
            {
                if (!uiObject.Visible)
                    continue;

                uiObject.Render(CalculateRect(uiObject), _buffer);
            }

            for (int i = 0; i < _buffer.Count; i++)
            {
                var cmd = _buffer[i];
                cmd.screenSpace = true;
                cmd.order += OrderBase;
                _system.Submit(cmd);
            }
        }

        public override void Dispose()
        {
            _canvases.Remove(this);

            foreach (var uiObject in _uiObjects)
                uiObject.Dispose();

            _uiObjects.Clear();
        }
    }
}

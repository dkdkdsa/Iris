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

            var size = uiObject.GetSize() * scale;
            var frame = CalculateFrame(uiObject.Parent);

            ResolveAxis(frame.Origin.X, frame.Size.X, uiObject.Anchor.X, uiObject.AnchorMax.X,
                uiObject.Position.X, uiObject.OffsetMax.X, size.X, scale, out float x, out float width);

            ResolveAxis(frame.Origin.Y, frame.Size.Y, uiObject.Anchor.Y, uiObject.AnchorMax.Y,
                uiObject.Position.Y, uiObject.OffsetMax.Y, size.Y, scale, out float y, out float height);

            return new Rectangle<float>(x, y, width, height);
        }

        private static void ResolveAxis(float frameOrigin, float frameSize, float anchorMin, float anchorMax,
            float position, float offsetMax, float size, float scale, out float origin, out float length)
        {
            float minPoint = frameOrigin + frameSize * anchorMin;

            if (anchorMax > anchorMin)
            {
                float maxPoint = frameOrigin + frameSize * anchorMax;

                origin = minPoint + position * scale;
                length = Math.Max(0f, maxPoint - offsetMax * scale - origin);
                return;
            }

            length = Math.Abs(size);

            float pivot = size < 0f ? 1f - anchorMin : anchorMin;
            origin = minPoint + position * scale - length * pivot;
        }

        private Rectangle<float> CalculateFrame(UIObject parent)
        {
            if (parent != null)
                return CalculateRect(parent);

            var viewport = _system.Viewport;
            return new Rectangle<float>(0f, 0f, viewport.X, viewport.Y);
        }

        public UIObject HitTest(Vector2D<float> screenPosition)
        {
            if (_system == null)
                return null;

            UIObject best = null;
            int bestOrder = int.MinValue;

            foreach (var uiObject in _uiObjects)
            {
                if (!uiObject.IsVisibleInHierarchy)
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

            if (Layout != null)
                ApplyLayout();
        }

        protected override void OnEnable()
        {
            if (!_canvases.Contains(this))
                _canvases.Add(this);
        }

        protected override void OnDisable()
        {
            _canvases.Remove(this);
        }

        public override void LateUpdate()
        {
            if (_system == null)
                return;

            _buffer.Clear();

            foreach (var uiObject in _uiObjects)
            {
                if (!uiObject.IsVisibleInHierarchy)
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

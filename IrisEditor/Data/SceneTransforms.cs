using Iris.Core;
using System;
using System.Numerics;

namespace IrisEditor.Data
{
    internal static class SceneTransforms
    {
        private const int MaxDepth = 64;

        public static ActorData FindParent(SceneData scene, ActorData actor)
        {
            if (actor?.ParentId is not Guid id)
                return null;

            return scene.Actors.Find(x => x.Id == id);
        }

        public static bool IsAncestorOrSelf(SceneData scene, ActorData ancestor, ActorData actor)
        {
            var current = actor;

            for (int i = 0; i < MaxDepth && current != null; i++)
            {
                if (current == ancestor)
                    return true;

                current = FindParent(scene, current);
            }

            return false;
        }

        public static (Vector2 Position, float Rotation, Vector2 Scale) GetWorld(SceneData scene, ActorData actor)
        {
            return GetWorld(scene, actor, 0);
        }

        private static (Vector2 Position, float Rotation, Vector2 Scale) GetWorld(SceneData scene, ActorData actor, int depth)
        {
            var transform = actor.GetComponent(typeof(Transform));
            var localPosition = transform?.GetVector2("LocalPosition", Vector2.Zero) ?? Vector2.Zero;
            float localRotation = transform?.GetFloat("LocalRotation", 0f) ?? 0f;
            var localScale = transform?.GetVector2("LocalScale", Vector2.One) ?? Vector2.One;

            var parent = FindParent(scene, actor);

            if (parent == null || depth >= MaxDepth)
                return (localPosition, localRotation, localScale);

            var (parentPosition, parentRotation, parentScale) = GetWorld(scene, parent, depth + 1);

            var position = parentPosition + Rotate(localPosition * parentScale, parentRotation);
            float rotation = parentRotation + localRotation;
            var scale = parentScale * localScale;

            return (position, rotation, scale);
        }

        public static void SetWorldPosition(SceneData scene, ActorData actor, Vector2 world)
        {
            var transform = actor.GetComponent(typeof(Transform));

            if (transform == null)
                return;

            var parent = FindParent(scene, actor);

            if (parent == null)
            {
                transform.SetVector2("LocalPosition", world);
                return;
            }

            var (parentPosition, parentRotation, parentScale) = GetWorld(scene, parent, 0);
            var delta = Rotate(world - parentPosition, -parentRotation);

            transform.SetVector2("LocalPosition", SafeDivide(delta, parentScale));
        }

        public static void Reparent(SceneData scene, ActorData actor, ActorData newParent)
        {
            if (actor == null)
                return;

            if (newParent != null && IsAncestorOrSelf(scene, actor, newParent))
                return;

            if (actor.ParentId == newParent?.Id)
                return;

            var (position, rotation, scale) = GetWorld(scene, actor, 0);

            actor.ParentId = newParent?.Id;

            var transform = actor.GetComponent(typeof(Transform));

            if (transform == null)
                return;

            if (newParent == null)
            {
                transform.SetVector2("LocalPosition", position);
                transform.SetFloat("LocalRotation", rotation);
                transform.SetVector2("LocalScale", scale);
                return;
            }

            var (parentPosition, parentRotation, parentScale) = GetWorld(scene, newParent, 0);
            var delta = Rotate(position - parentPosition, -parentRotation);

            transform.SetVector2("LocalPosition", SafeDivide(delta, parentScale));
            transform.SetFloat("LocalRotation", rotation - parentRotation);
            transform.SetVector2("LocalScale", SafeDivide(scale, parentScale));
        }

        private static Vector2 SafeDivide(Vector2 value, Vector2 divisor)
        {
            return new Vector2(
                divisor.X != 0f ? value.X / divisor.X : 0f,
                divisor.Y != 0f ? value.Y / divisor.Y : 0f);
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            if (degrees == 0f)
                return vector;

            float rad = degrees * MathF.PI / 180f;
            float cos = MathF.Cos(rad);
            float sin = MathF.Sin(rad);

            return new Vector2(
                vector.X * cos + vector.Y * sin,
                -vector.X * sin + vector.Y * cos);
        }
    }
}

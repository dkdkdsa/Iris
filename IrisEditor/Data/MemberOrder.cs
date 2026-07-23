using System;
using System.Reflection;

namespace IrisEditor.Data
{
    internal static class MemberOrder
    {
        public static long KeyOf(PropertyInfo property)
        {
            var backing = property.DeclaringType?.GetField(
                $"<{property.Name}>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return backing != null
                ? Key(property.DeclaringType, backing.MetadataToken)
                : Key(property.DeclaringType, property.MetadataToken);
        }

        public static long KeyOf(FieldInfo field)
        {
            return Key(field.DeclaringType, field.MetadataToken);
        }

        private static long Key(Type declaringType, int token)
        {
            return ((long)Depth(declaringType) << 32) | (uint)token;
        }

        private static int Depth(Type type)
        {
            int depth = 0;

            for (var current = type; current != null; current = current.BaseType)
                depth++;

            return depth;
        }
    }
}

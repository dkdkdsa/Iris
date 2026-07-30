using Iris.Core;

namespace Iris.Debugging
{
    public static class LogContext
    {
        public static string Describe(object context)
        {
            if (context == null)
                return null;

            if (context is Actor actor)
                return actor.Name;

            if (context is Component component)
                return component.OwnerActor != null
                    ? $"{component.OwnerActor.Name}.{component.GetType().Name}"
                    : component.GetType().Name;

            return context.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public static class Factories
    {
        private static readonly Dictionary<FactoryKey, object> _factories = new();

        public static void Register<TProduct, TRequest>(IFactory<TProduct, TRequest> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _factories[FactoryKey.Of<TProduct, TRequest>()] = factory;
        }

        public static bool Unregister<TProduct, TRequest>()
        {
            return _factories.Remove(FactoryKey.Of<TProduct, TRequest>());
        }

        public static bool TryGetFactory<TProduct, TRequest>(out IFactory<TProduct, TRequest> factory)
        {
            if (_factories.TryGetValue(FactoryKey.Of<TProduct, TRequest>(), out object stored))
            {
                factory = (IFactory<TProduct, TRequest>)stored;
                return true;
            }

            factory = null;
            return false;
        }


        public static IFactory<TProduct, TRequest> GetFactory<TProduct, TRequest>()
        {
            if (TryGetFactory<TProduct, TRequest>(out var factory))
            {
                return factory;
            }

            return null;
        }

        public static TProduct Create<TProduct, TRequest>(TRequest req)
        {
            if(TryGetFactory<TProduct, TRequest>(out var factory))
            {
                return factory.Create(req);
            }

            return default;
        }
    }
}

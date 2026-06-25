using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    internal readonly struct FactoryKey : IEquatable<FactoryKey>
    {
        private readonly Type _product;
        private readonly Type _request;

        private FactoryKey(Type product, Type request)
        {
            _product = product;
            _request = request;
        }

        public static FactoryKey Of<TProduct, TRequest>()
        {
            return new FactoryKey(typeof(TRequest), typeof(TProduct));
        }

        public bool Equals(FactoryKey other)
        {
            return _product == other._product && _request == other._request;
        }

        public override bool Equals(object obj)
        {
            return obj is FactoryKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_request.GetHashCode() * 397) ^ _product.GetHashCode();
            }
        }
    }
}

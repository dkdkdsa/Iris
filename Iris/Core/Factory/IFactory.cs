using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public interface IFactory<out TProduct, in TRequest>
    {
        public TProduct Create(TRequest request);
    }
}

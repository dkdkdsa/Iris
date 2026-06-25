using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Platform
{
    public interface IWindow : IDisposable
    {
        public int Width { get; }
        public int Height { get; }
    }
}

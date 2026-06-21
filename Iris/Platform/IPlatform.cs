using Iris.Core;
using Iris.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Platform
{
    public interface IPlatform : IDisposable
    {

        internal IWindow Window { get; }
        internal IRenderBackend Backend { get; }
        internal void CreateWindow(WindowConfig config);
        internal void PumpEvents();
        internal bool IsCloseRequested { get; }
    }
}

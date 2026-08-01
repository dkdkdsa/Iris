using Iris.Audio;
using Iris.Core;
using Iris.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Platform
{
    public interface IPlatform : IDisposable
    {
        public event Action<FileDropEvent> FileDropped;

        public IWindow Window { get; }
        public IRenderBackend RenderBackend { get; }
        public IAudioBackend AudioBackend { get; }
        public IInputBackend InputBackend { get; }
        public IClipboard Clipboard { get; }
        public void CreateWindow(WindowConfig config);
        public void PumpEvents();
        public bool IsCloseRequested { get; }
    }
}

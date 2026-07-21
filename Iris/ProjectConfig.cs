using System;
using System.Collections.Generic;
using System.Text;

namespace Iris
{
    public sealed class ProjectConfig
    {
        public string StartScene;
        public int DefaultWidth = 1280;
        public int DefaultHeight = 720;
        public bool Fullscreen = false;
        public bool Resizable = false;
        public string Title = "NewGame";
    }
}

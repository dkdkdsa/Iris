using System;
using System.Collections.Generic;
using System.Text;

namespace Iris
{
    public sealed class ProjectConfig
    {
        public int DefaultWidth = 1280;
        public int DefaultHeight = 720;
        public bool Fullscreen = false;
        public bool Resizable = false;
        public bool VSync = true;
        public int TargetFrameRate = 0;
        public bool LogToFile = true;
        public bool Stats = false;
        public bool BatchByTexture = false;
        public string Title = "NewGame";
        public List<string> BuildScenes = new();
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public static class Time
    {
        public static float DeltaTime { get; internal set; }
        public static float FixedTimeStep { get; set; } = 1f / 60f;
    }
}

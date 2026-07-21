using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.UI
{
    public class UIObject : IDisposable
    {
        public int Order { get; set; } = 0;

        public void Render()
        {

        }


        public void Dispose()
        {
        }
    }
}

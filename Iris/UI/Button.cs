using Iris.UI.Events;
using System;

namespace Iris.UI
{
    public class Button : UIObject, IPointerClick
    {
        public event Action Clicked;

        public void OnPointerClick(PointerEventArgs args)
        {
            args.Handled = true;
            Clicked?.Invoke();
        }
    }
}

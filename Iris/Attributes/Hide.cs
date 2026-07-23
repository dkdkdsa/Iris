using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class HideAttribute : Attribute
    {
    }
}

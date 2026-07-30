using System;

namespace IrisEditor.Workspace
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class AssetCreatorAttribute : Attribute
    {
        public string MenuName { get; }
        public string DefaultFileName { get; }

        public AssetCreatorAttribute(string menuName, string defaultFileName)
        {
            MenuName = menuName;
            DefaultFileName = defaultFileName;
        }
    }
}

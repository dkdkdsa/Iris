using System;

namespace IrisEditor.Workspace
{
    // static void (string path) 시그니처의 정적 메서드에 붙이면
    // 프로젝트 창 우클릭 > 생성 메뉴에 자동 등록된다.
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

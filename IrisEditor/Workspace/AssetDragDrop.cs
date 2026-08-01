namespace IrisEditor.Workspace
{
    internal static class AssetDragDrop
    {
        public const string PayloadType = "IRIS_ASSET";

        public static AssetEntry Current;

        public static string DirectoryPath;

        public static void SetAsset(AssetEntry asset)
        {
            Current = asset;
            DirectoryPath = null;
        }

        public static void SetDirectory(string relativePath)
        {
            Current = null;
            DirectoryPath = relativePath;
        }
    }
}

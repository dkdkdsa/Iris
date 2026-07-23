namespace Iris.Core
{
    public static class SceneManager
    {
        public static Scene Active => SystemManager.Instance.GetSystem<SceneSystem>().ActiveScene;

        public static Scene Load(string path)
        {
            var scene = SceneLoader.Load(path);
            SystemManager.Instance.GetSystem<SceneSystem>().LoadScene(scene);
            return scene;
        }

        public static void Load(Scene scene)
        {
            SystemManager.Instance.GetSystem<SceneSystem>().LoadScene(scene);
        }
    }
}

using System;

namespace Iris.Core
{
    public sealed class SceneSystem : SystemBase
    {
        public Scene ActiveScene { get; private set; }

        public void LoadScene(Scene scene)
        {
            ActiveScene?.Dispose();
            ActiveScene = scene;
        }

        public override void Update()
        {
            try
            {
                ActiveScene?.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public override void FixedUpdate()
        {
            try
            {
                ActiveScene?.FixedUpdate();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public override void LateUpdate()
        {
            try
            {
                ActiveScene?.LateUpdate();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public override void Dispose()
        {
            try
            {
                ActiveScene?.Dispose();
                ActiveScene = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
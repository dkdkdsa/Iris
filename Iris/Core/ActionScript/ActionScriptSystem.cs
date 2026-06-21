using System;
using System.Collections.Generic;
using System.Text;

namespace Iris.Core
{
    public class ActionScriptSystem : SystemBase
    {
        private List<ActionScript> _actionScripts = new List<ActionScript>();

        public ActionScriptSystem()
        {
        }

        public T CreateActionScript<T>() where T : ActionScript, new()
        {
            T newScript = new T();
            newScript.OnCreate();
            AddActionScript(newScript);
            return newScript;
        }

        private void AddActionScript(ActionScript script)
        {
            _actionScripts.Add(script);
        }

        public override void Update()
        {
            foreach(var script in _actionScripts)
            {
                script.OnUpdate();
            }
        }

        public override void FixedUpdate()
        {
            foreach (var script in _actionScripts)
            {
                script.OnFixedUpdate();
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace IrisEditor.Data
{
    internal class ActorData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public string PrefabSource { get; set; }
        public List<ComponentData> Components { get; set; } = new();

        public ComponentData GetComponent(Type type)
        {
            return Components.Find(x => x.TargetType == type);
        }
    }
}

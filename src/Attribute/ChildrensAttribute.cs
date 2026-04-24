using UnityEngine;

namespace MornLib
{
    public sealed class ChildrensAttribute : PropertyAttribute
    {
        public readonly string Name;
        public readonly bool Deep;

        public ChildrensAttribute()
        {
        }

        public ChildrensAttribute(bool deep)
        {
            Deep = deep;
        }

        public ChildrensAttribute(string name, bool deep = false)
        {
            Name = name;
            Deep = deep;
        }
    }
}

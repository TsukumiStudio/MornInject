using UnityEngine;

namespace MornLib
{
    public sealed class ChildAttribute : PropertyAttribute
    {
        public readonly string Name;
        public readonly bool Deep;

        public ChildAttribute()
        {
        }

        public ChildAttribute(bool deep)
        {
            Deep = deep;
        }

        public ChildAttribute(string name, bool deep = false)
        {
            Name = name;
            Deep = deep;
        }
    }
}

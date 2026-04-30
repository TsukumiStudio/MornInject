using UnityEngine;

namespace MornLib
{
    public sealed class ChildrensAttribute : PropertyAttribute
    {
        public readonly string Name;
        public readonly bool Deep;
        public readonly bool IncludeSelf;

        public ChildrensAttribute()
        {
        }

        public ChildrensAttribute(bool deep)
        {
            Deep = deep;
        }

        public ChildrensAttribute(bool deep, bool includeSelf)
        {
            Deep = deep;
            IncludeSelf = includeSelf;
        }

        public ChildrensAttribute(string name, bool deep = false)
        {
            Name = name;
            Deep = deep;
        }
    }
}

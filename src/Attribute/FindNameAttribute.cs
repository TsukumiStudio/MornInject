using UnityEngine;

namespace MornLib
{
    public sealed class FindNameAttribute : PropertyAttribute
    {
        public readonly string Name;

        public FindNameAttribute(string name)
        {
            Name = name;
        }
    }
}

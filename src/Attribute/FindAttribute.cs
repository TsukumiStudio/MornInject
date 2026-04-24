using UnityEngine;

namespace MornLib
{
    public sealed class FindAttribute : PropertyAttribute
    {
        public readonly string Name;

        public FindAttribute(string name = null)
        {
            Name = name;
        }
    }
}

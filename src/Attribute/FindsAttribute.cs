using UnityEngine;

namespace MornLib
{
    public sealed class FindsAttribute : PropertyAttribute
    {
        public readonly string Name;

        public FindsAttribute(string name = null)
        {
            Name = name;
        }
    }
}

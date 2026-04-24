using UnityEngine;

namespace MornLib
{
    public sealed class FindAssetNameAttribute : PropertyAttribute
    {
        public readonly string Name;

        public FindAssetNameAttribute(string name)
        {
            Name = name;
        }
    }
}

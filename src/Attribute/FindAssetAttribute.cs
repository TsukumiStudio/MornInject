using UnityEngine;

namespace MornLib
{
    public sealed class FindAssetAttribute : PropertyAttribute
    {
        public readonly string Name;

        public FindAssetAttribute(string name = null)
        {
            Name = name;
        }
    }
}

using UnityEngine;

namespace MornLib
{
    public sealed class FindAssetsAttribute : PropertyAttribute
    {
        public readonly string Name;

        public FindAssetsAttribute(string name = null)
        {
            Name = name;
        }
    }
}

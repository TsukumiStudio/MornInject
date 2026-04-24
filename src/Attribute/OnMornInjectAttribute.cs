using System;

namespace MornLib
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OnMornInjectAttribute : Attribute
    {
    }
}

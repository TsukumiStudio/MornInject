using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("MornInject.Editor")]
namespace MornLib
{
    internal static class MornInjectLogger
    {
        private static string ModuleName => "MornInject";
        private static string Prefix => $"[<color=#{ColorUtility.ToHtmlStringRGB(Color.cyan)}>{ModuleName}</color>] ";

        public static void Log(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }
    }
}

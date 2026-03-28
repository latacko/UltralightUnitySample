using UnityEngine;

namespace Latacko.UltralightUnity
{
    public static class KeyCodeExtensions
    {
        public static UltralightSharedClasses.Enums.KeyCode ToUltralight(this KeyCode key)
        {
            return (UltralightSharedClasses.Enums.KeyCode)((uint)key);
        }
    }
}
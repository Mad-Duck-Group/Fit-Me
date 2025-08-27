using UnityEngine;

namespace MadDuck.Scripts.Utils
{
    public static class InterfaceUtils
    {
        public static T InstantiateAsInterface<T>(this T prefab, InstantiateParameters parameters, out GameObject gameObject) where T : class
        {
            if (prefab is not MonoBehaviour monoBehaviour)
            {
                Debug.LogError($"Prefab of type {typeof(T)} is not a MonoBehaviour. Cannot instantiate.");
                gameObject = null;
                return null;
            }
            var clone = Object.Instantiate(monoBehaviour, parameters);
            gameObject = clone.gameObject;
            return clone as T;
        }
    }
}
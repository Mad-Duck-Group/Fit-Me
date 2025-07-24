using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Others
{
    public class TestBounds : MonoBehaviour
    {
        [Button("Debug Bounds")]
        private void DebugBounds()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogError("SpriteRenderer component not found on this GameObject.");
                return;
            }
            Bounds bounds = renderer.bounds;
            Debug.Log($"Bounds Center: {bounds.center}, Size: {bounds.size}, Extents: {bounds.extents}");
        }
    }
}
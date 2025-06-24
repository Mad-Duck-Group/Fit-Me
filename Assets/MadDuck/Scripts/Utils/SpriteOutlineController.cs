using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.Utils
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteOutlineController : MonoBehaviour
    {
        [Title("Outline Settings")]
        public Color outlineColor = Color.black;
        [PropertyRange(0, 0.5f)] public float outlineWidth = 0.05f;
    
        [Title("Edge Toggles")]
        public bool outlineTop = true;
        public bool outlineBottom = true;
        public bool outlineLeft = true;
        public bool outlineRight = true;

        private MaterialPropertyBlock _propBlock;
        private SpriteRenderer _spriteRenderer;
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int OutlineTop = Shader.PropertyToID("_OutlineTop");
        private static readonly int OutlineBottom = Shader.PropertyToID("_OutlineBottom");
        private static readonly int OutlineLeft = Shader.PropertyToID("_OutlineLeft");
        private static readonly int OutlineRight = Shader.PropertyToID("_OutlineRight");

        void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            UpdateOutline();
        }

        void OnValidate()
        {
            if (_spriteRenderer == null) 
                _spriteRenderer = GetComponent<SpriteRenderer>();
        
            UpdateOutline();
        }

        public void UpdateOutline()
        {
            if (_spriteRenderer == null) return;
            _propBlock ??= new MaterialPropertyBlock();
            _spriteRenderer.GetPropertyBlock(_propBlock);
        
            // Apply outline settings
            _propBlock.SetColor(OutlineColor, outlineColor);
            _propBlock.SetFloat(OutlineWidth, outlineWidth);
        
            // Convert bools to float (1 or 0) for shader
            _propBlock.SetFloat(OutlineTop, outlineTop ? 1 : 0);
            _propBlock.SetFloat(OutlineBottom, outlineBottom ? 1 : 0);
            _propBlock.SetFloat(OutlineLeft, outlineLeft ? 1 : 0);
            _propBlock.SetFloat(OutlineRight, outlineRight ? 1 : 0);
        
            _spriteRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
using UnityEngine;

namespace MadDuck.Scripts.Units
{
    public class Atom : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;
        private Block _parentBlock;
        public Block ParentBlock {get => _parentBlock; set => _parentBlock = value;}
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}

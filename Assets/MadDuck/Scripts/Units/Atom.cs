using MadDuck.Scripts.Utils;
using UnityEngine;

namespace MadDuck.Scripts.Units
{
    public class Atom : MonoBehaviour
    {
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Block ParentBlock { get; set; }
        public SpriteOutlineController SpriteOutlineController { get; private set; }

        void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
            SpriteOutlineController = GetComponent<SpriteOutlineController>();
        }
    }
}

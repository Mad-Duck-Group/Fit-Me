using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.Units
{
    public class Cell : MonoBehaviour
    {
        #region Inspectors
        [Title("References")]
        [SerializeField] private Sprite[] whitePatterns;
        [SerializeField] private Sprite[] blackPatterns;
        
        [field: Title("Debug")]
        [field: SerializeField, ReadOnly] public Atom CurrentAtom { get; private set; }
        #endregion
        
        #region Fields and Properties
        public SpriteRenderer SpriteRenderer { get; private set; }
        public Color OriginalColor { get; set; }
        public Vector2Int ArrayIndex { get; set; }
        public Vector2Int GridIndex { get; set; }
        #endregion
        
        void Awake()
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
            OriginalColor = SpriteRenderer.color;
        }

        /// <summary>
        /// Set the atom of the cell
        /// </summary>
        /// <param name="atom">Atom to set</param>
        public void SetAtom(Atom atom)
        {
            CurrentAtom = atom;
        }

        public void SetPattern(int row, int column)
        {
            //white first
            if (row % 2 == 0)
            {
                SpriteRenderer.sprite = column % 2 == 0
                    ? whitePatterns[0]
                    : blackPatterns[0];
            }
            //black first
            else
            {
                SpriteRenderer.sprite = column % 2 == 0
                    ? blackPatterns[1]
                    : whitePatterns[1];
            }
        }
    }
}

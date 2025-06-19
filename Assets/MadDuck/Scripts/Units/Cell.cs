using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.Units
{
    public class Cell : MonoBehaviour
    {
        #region Inspectors
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
        }

        /// <summary>
        /// Set the atom of the cell
        /// </summary>
        /// <param name="atom">Atom to set</param>
        public void SetAtom(Atom atom)
        {
            CurrentAtom = atom;
        }
    }
}

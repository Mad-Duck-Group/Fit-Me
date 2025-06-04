using System.Collections;
using System.Collections.Generic;
using MadDuck.Scripts.Units;
using Sirenix.OdinInspector;
using UnityEngine;

public class Cell : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    public Color OriginalColor { get; set; }
    public Vector2Int ArrayIndex { get; set; }
    public Vector2Int GridIndex { get; set; }
    [SerializeField][ReadOnly] private Atom currentAtom;
    
    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    

    public Atom CurrentAtom => currentAtom;
    // Start is called before the first frame update
    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Set the atom of the cell
    /// </summary>
    /// <param name="atom">Atom to set</param>
    public void SetAtom(Atom atom)
    {
        currentAtom = atom;
    }
}

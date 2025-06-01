using System.Collections;
using System.Collections.Generic;
using MadDuck.Scripts.Units;
using Sirenix.OdinInspector;
using UnityEngine;

public class Cell : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;
    private int[] _index;
    [SerializeField][ReadOnly] private Atom currentAtom;
    
    public SpriteRenderer SpriteRenderer => _spriteRenderer;
    public Color OriginalColor {get => _originalColor; set => _originalColor = value;}
    public int[] Index {get => _index; set => _index = value;}
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

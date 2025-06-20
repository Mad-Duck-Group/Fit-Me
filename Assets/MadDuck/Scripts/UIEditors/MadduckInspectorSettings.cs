using UnityEngine;

namespace MadDuck.Scripts.UIEditors
{
    [CreateAssetMenu(fileName = "MadduckInspectorSettings", menuName = "Scriptable Objects/MadduckInspectorSettings")]
    public class MadduckInspectorSettings : ScriptableObject
    {
        [SerializeField] public bool gameDesignerMode;
    }
}

using UnityEngine;

namespace MadDuck.Scripts.UIEditors
{
    [CreateAssetMenu(fileName = "MadduckInspectorSettings", menuName = "MadDuck/Settings/MadduckInspectorSettings")]
    public class MadduckInspectorSettings : ScriptableObject
    {
        [SerializeField] public bool gameDesignerMode;
    }
}

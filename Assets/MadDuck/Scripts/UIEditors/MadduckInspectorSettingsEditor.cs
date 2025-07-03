using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MadDuck.Scripts.UIEditors
{ 
    #if UNITY_EDITOR
    public class MadduckInspectorSettingsEditor : EditorWindow
    {   
        [SerializeField]
        private VisualTreeAsset visualTreeAsset;
        
        [MenuItem("Tools/Madduck Inspector Settings")]
        public static void OpenSettingsWindow()
        {
            var window = GetWindow<MadduckInspectorSettingsEditor>("Madduck Inspector Settings");
            window.titleContent = new GUIContent("Madduck Inspector Settings");
        }
        
        public void CreateGUI()
        {
            visualTreeAsset.CloneTree(rootVisualElement);
        }
    }
    #endif
}

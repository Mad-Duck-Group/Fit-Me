using MadDuck.Scripts.Tutorials;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    public class TutorialManager : MonoSingleton<TutorialManager>
    {
        [Title("State Machine")] 
        [SerializeReference, HideReferenceObjectPicker] private TutorialStateMachine tutorialStateMachine = new();

        [Title("Settings")] 
        [SerializeField] private float startDelay = 1f;
        
        [Title("Panels")]
        [SerializeReference, HideReferenceObjectPicker] private UIPanelController panelController = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule tutorialScreen = new ();
    }
}
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;

namespace MadDuck.Scripts.Managers
{
    public enum MainMenuPanelType
    {
        SplashScreen,
        TermsAndConditions,
        MainMenu,
        Stats,
        Achievements,
        Settings,
    }
    public class MainMenuManager : MonoSingleton<MainMenuManager>
    {
        [Title("References")] 
        [field: SerializeField] public SerializableDictionary<MainMenuPanelType, UIPanel> PanelDictionary { get; private set; }
        [SerializeReference, HideReferenceObjectPicker, HideLabel] private UIPanelController panelController = new();

        private void Start()
        {
            panelController.ChangePanel(PanelDictionary[MainMenuPanelType.SplashScreen]).Forget();
        }
    }
}

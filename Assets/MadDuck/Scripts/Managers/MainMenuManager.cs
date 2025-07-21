using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using Redcode.Extensions;
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
        
        [Title("Settings")]
        [SerializeField] private MainMenuPanelType initialPanelType = MainMenuPanelType.SplashScreen;

        private void Start()
        {
            PanelDictionary.Values.ForEach(p =>
            {
                p.Initialize();
            });
            panelController.ShowPanel(PanelDictionary[initialPanelType]).Forget();
        }
    }
}

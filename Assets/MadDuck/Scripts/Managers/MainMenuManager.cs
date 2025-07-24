using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Transitions;
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
        [field: SerializeReference, HideReferenceObjectPicker, HideLabel] public UIPanelController PanelController { get; private set; } = new();
        
        [Title("Settings")]
        [SerializeField] private MainMenuPanelType initialPanelType = MainMenuPanelType.SplashScreen;

        private void OnEnable()
        {
            LoadSceneManager.OnFinishFadeIn += OnFinishFadeIn;
        }

        private void OnDisable()
        {
            LoadSceneManager.OnFinishFadeIn -= OnFinishFadeIn;
        }

        private void OnFinishFadeIn()
        {
            if (LoadSceneManager.FirstSceneLoaded) initialPanelType = MainMenuPanelType.MainMenu;
            ShowFirstPanel();
        }

        private void Start()
        {
            PanelDictionary.Values.ForEach(p =>
            {
                p.Initialize();
                p.PanelController = PanelController;
            });
        }

        private void ShowFirstPanel()
        {
            PanelController.ShowPanel(PanelDictionary[initialPanelType], cancellationToken: destroyCancellationToken).Forget();
        }
    }
}

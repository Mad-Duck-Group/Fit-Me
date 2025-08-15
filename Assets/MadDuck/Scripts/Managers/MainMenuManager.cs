using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.UIs.Panels;
using MadDuck.Scripts.UIs.Transitions;
using MessagePipe;
using Redcode.Extensions;
using Sherbert.Framework.Generic;
using Sirenix.OdinInspector;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public struct SceneActivateEvent
    {
        public readonly SceneType sceneType;
        public SceneActivateEvent(SceneType sceneType)
        {
            this.sceneType = sceneType;
        }
    }
    
    public class MainMenuManager : MonoSingleton<MainMenuManager>
    {
        [Title("References")]
        [field: SerializeField] public SerializableDictionary<MainMenuPanelType, UIPanel> PanelDictionary { get; private set; }
        [field: SerializeReference, HideReferenceObjectPicker, HideLabel] public UIPanelController PanelController { get; private set; } = new();
        
        [Title("Settings")]
        [SerializeField] private MainMenuPanelType initialPanelType = MainMenuPanelType.SplashScreen;

        private void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        }

        private void OnEnable()
        {
            LoadSceneManager.OnFinishLoad += OnFinishLoad;
            LoadSceneManager.OnStartFadeOut += OnStartFadeOut;
            GridManager.OnFitCheck += OnFitCheck;
        }

        private void OnDisable()
        {
            LoadSceneManager.OnFinishLoad -= OnFinishLoad;
            LoadSceneManager.OnStartFadeOut -= OnStartFadeOut;
            GridManager.OnFitCheck -= OnFitCheck;
        }
        
        private void OnStartFadeOut()
        {
            PanelDictionary.Values.ForEach(x => x.DeactivateInput());
        }
        
        private void OnFinishLoad()
        {
            if (LoadSceneManager.FirstSceneLoaded) initialPanelType = MainMenuPanelType.MainMenu;
            ShowFirstPanel();
        }
        
        private void OnFitCheck(FitType fitType)
        {
            if (fitType is not FitType.FitMe) return;
            LoadSceneManager.Instance.LoadScene(SceneType.Gameplay, LoadSceneMode.Single, false).Forget();
        }

        private void ShowFirstPanel()
        {
            PanelDictionary.Values.ForEach(p =>
            {
                p.Initialize();
                p.PanelController = PanelController;
            });
            PanelController.ShowPanel(PanelDictionary[initialPanelType], cancellationToken: destroyCancellationToken).Forget();
        }
    }
}

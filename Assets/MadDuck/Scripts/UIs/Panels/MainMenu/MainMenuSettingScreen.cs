using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels
{
    [Serializable]
    public struct ToggleButton
    {
        [field: SerializeField] public Button Button { get; private set; }
        [field: SerializeField] public GameObject Slash { get; private set; } 
        [ShowInInspector, ReadOnly] public bool IsActivated
        {
            get => _isActivated;
            set
            { 
                _isActivated = value;
                Slash.SetActive(value);
            }
        }
        
        private bool _isActivated;

        public void Toggle()
        {
            IsActivated = !IsActivated;
            Slash.SetActive(IsActivated);
        }
    }
}
namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class MainMenuSettingScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button backButton;
        [SerializeField] private ToggleButton muteBgmButton;
        [SerializeField] private ToggleButton muteSfxButton;
        
        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();

        public override void Initialize()
        {
            base.Initialize();
            backButton.onClick.AddListener(OnBackButtonClicked);
            muteBgmButton.Button.onClick.AddListener(OnMuteBgmButtonClicked);
            muteSfxButton.Button.onClick.AddListener(OnMuteSfxButtonClicked);
            AudioManager.Instance.GetBusMuteState(BusType.BGM, out var muted);
            muteBgmButton.IsActivated = muted;
            AudioManager.Instance.GetBusMuteState(BusType.SFX, out muted);
            muteSfxButton.IsActivated = muted;
            
        }
        
        private void OnBackButtonClicked()
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, mainMenuCrossFadeRule.nextPanel, mainMenuCrossFadeRule.crossFadeSettings, 
                transitionCts.Token).Forget();
        }
        
        private void OnMuteBgmButtonClicked()
        {
            AudioManager.Instance.ToggleMuteBus(BusType.BGM);
            muteBgmButton.Toggle();
        }
        
        private void OnMuteSfxButtonClicked()
        {
            AudioManager.Instance.ToggleMuteBus(BusType.SFX);
            muteSfxButton.Toggle();
        }
    }
}
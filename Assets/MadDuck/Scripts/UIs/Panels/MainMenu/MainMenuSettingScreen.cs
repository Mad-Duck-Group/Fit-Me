using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class MainMenuSettingScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button muteBgmButton;
        [SerializeField] private Button muteSfxButton;
        
        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule mainMenuCrossFadeRule = new();

        public override void Initialize()
        {
            base.Initialize();
            backButton.onClick.AddListener(OnBackButtonClicked);
            muteBgmButton.onClick.AddListener(OnMuteBgmButtonClicked);
            muteSfxButton.onClick.AddListener(OnMuteSfxButtonClicked);
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
        }
        
        private void OnMuteSfxButtonClicked()
        {
            AudioManager.Instance.ToggleMuteBus(BusType.SFX);
        }
    }
}
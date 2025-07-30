using System.Threading;
using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.Gameplay
{
    [ShowOdinSerializedPropertiesInInspector]
    public class PausePanel : UIPanel
    {
        [Title("References")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private ToggleButton muteSfxButton;
        [SerializeField] private ToggleButton muteBgmButton;
    
        [Title("Panels")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule gameplayCrossFadeRule = new();

        public override void Initialize()
        {
            base.Initialize();
            resumeButton.onClick.AddListener(OnResumeButtonClicked);
            helpButton.onClick.AddListener(OnHelpButtonClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuButton);
            muteSfxButton.Button.onClick.AddListener(OnToggleMuteSFX);
            muteBgmButton.Button.onClick.AddListener(OnToggleMuteBGM);
            AudioManager.Instance.GetBusMuteState(BusType.BGM, out var muted);
            muteBgmButton.IsActivated = muted;
            AudioManager.Instance.GetBusMuteState(BusType.SFX, out muted);
            muteSfxButton.IsActivated = muted;
        }

        private void OnResumeButtonClicked()
        {
            GameManager.Instance.ResumeGame();
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, gameplayCrossFadeRule.nextPanel, gameplayCrossFadeRule.crossFadeSettings, transitionCts.Token).Forget();
        }

        private void OnHelpButtonClicked()
        {
            Debug.Log("Help button clicked");
        }
    
        private void OnMainMenuButton()
        {
            GameManager.Instance.BackToMenu();
        }
    
        public void OnToggleMuteSFX()
        {
            AudioManager.Instance.ToggleMuteBus(BusType.SFX);
            muteSfxButton.Toggle();
        }

        public void OnToggleMuteBGM()
        {
            AudioManager.Instance.ToggleMuteBus(BusType.BGM);
            muteBgmButton.Toggle();
        }
    }
}

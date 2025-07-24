using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using PrimeTween;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class MainMenuScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private TMP_Text versionText;

        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule settingsCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule statsCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule achievementsCrossFadeRule = new();
        
        [Title("Audio")] 
        [SerializeField] private EventReference mainMenuBgm;
        
        private AudioReference _mainMenuBgmReference;

        public override void Initialize()
        {
            base.Initialize();
            playButton.onClick.AddListener(ToGameplay);
            settingsButton.onClick.AddListener(() => OnButtonClicked(settingsCrossFadeRule));
            statsButton.onClick.AddListener(() => OnButtonClicked(statsCrossFadeRule));
            achievementsButton.onClick.AddListener(() => OnButtonClicked(achievementsCrossFadeRule));
            versionText.text = Application.version;
        }

        public override void OnPanelReady()
        {
            base.OnPanelReady();
            if (_mainMenuBgmReference.IsPlaying()) return;
            _mainMenuBgmReference = AudioManager.Instance.PlayAudio(mainMenuBgm, transform.position);
        }

        private void OnButtonClicked(CrossFadeRule rule)
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, rule.nextPanel, rule.crossFadeSettings, 
                transitionCts.Token).Forget();
        }

        private void ToGameplay()
        {
            _mainMenuBgmReference.Stop();
            DeactivateInput();
            LoadSceneManager.Instance.LoadScene(SceneType.Gameplay, LoadSceneMode.Single, false);
        }
    }
}
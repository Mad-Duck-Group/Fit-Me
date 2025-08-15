using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using MadDuck.Scripts.Managers;
using MadDuck.Scripts.UIs.Transitions;
using MessagePipe;
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
        [SerializeField] private RectTransform logo;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private Transform sceneObjectsParent;

        [Title("Tween")]
        [SerializeField] private TweenSettings<Vector3> logoScaleTweenSettings;
        
        [Title("Panel")]
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule settingsCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule statsCrossFadeRule = new();
        [OdinSerialize, HideReferenceObjectPicker] private CrossFadeRule achievementsCrossFadeRule = new();
        
        [Title("Audio")] 
        [SerializeField] private EventReference mainMenuBgm;
        
        private AudioReference _mainMenuBgmReference;
        private Tween _logoTween;
        private IPublisher<SceneActivateEvent> _sceneActivatePublisher;
        
        private void OnEnable()
        {
            _sceneActivatePublisher = GlobalMessagePipe.GetPublisher<SceneActivateEvent>();
            LoadSceneManager.OnStartFadeOut += OnSwitchScene;
        }
        
        private void OnDisable()
        {
            LoadSceneManager.OnStartFadeOut -= OnSwitchScene;
        }

        public override void Initialize()
        {
            base.Initialize();
            settingsButton.onClick.AddListener(() => OnButtonClicked(settingsCrossFadeRule));
            statsButton.onClick.AddListener(() => OnButtonClicked(statsCrossFadeRule));
            achievementsButton.onClick.AddListener(() => OnButtonClicked(achievementsCrossFadeRule));
            versionText.text = Application.version;
        }

        public override void Show()
        {
            base.Show();
            sceneObjectsParent.gameObject.SetActive(true);
            _sceneActivatePublisher.Publish(new SceneActivateEvent(SceneType.MainMenu));
        }

        public override void Hide()
        {
            base.Hide();
            sceneObjectsParent.gameObject.SetActive(false);
            _mainMenuBgmReference.Stop();
        }
        
        public override void OnPanelReady()
        {
            base.OnPanelReady();
            if (_mainMenuBgmReference.IsPlaying()) return;
            _mainMenuBgmReference = AudioManager.Instance.PlayAudio(mainMenuBgm, transform.position);
            _logoTween = Tween.Scale(logo, logoScaleTweenSettings);
        }

        private void OnButtonClicked(CrossFadeRule rule)
        {
            transitionCts = new CancellationTokenSource();
            PanelController.ChangePanel(this, rule.nextPanel, rule.crossFadeSettings, 
                transitionCts.Token).Forget();
        }

        private void OnSwitchScene()
        {
            _mainMenuBgmReference.Stop();
            _logoTween.Stop();
            logo.localScale = Vector3.one;
        }
    }
}
using MadDuck.Scripts.Managers;
using PrimeTween;
using Sirenix.OdinInspector;
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

        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        
        public override void Initialize()
        {
            base.Initialize();
            playButton.onClick.AddListener(() => LoadSceneManager.Instance.LoadScene(SceneType.Gameplay, LoadSceneMode.Single, false));
            settingsButton.onClick.AddListener(() => OnButtonClicked(MainMenuPanelType.Settings));
            statsButton.onClick.AddListener(() => OnButtonClicked(MainMenuPanelType.Stats));
            achievementsButton.onClick.AddListener(() => OnButtonClicked(MainMenuPanelType.Achievements));
            versionText.text = Application.version;
        }

        private void OnButtonClicked(MainMenuPanelType mainMenuPanelType)
        {
            ChangePanel(this, MainMenuManager.Instance.PanelDictionary[mainMenuPanelType], CrossFadeSettings);
        }

        public override Sequence TransitionIn()
        {
            TransitionState = TransitionState.TransitioningIn;
            transitionSequence = Sequence.Create()
                .Group(Tween.Alpha(panelCanvasGroup, transitionInTweenSettings))
                .OnComplete(() =>
                {
                    TransitionState = TransitionState.Idle;
                });
            return transitionSequence;
        }
        
        public override Sequence TransitionOut()
        {
            TransitionState = TransitionState.TransitioningIn;
            transitionSequence = Sequence.Create()
                .Group(Tween.Alpha(panelCanvasGroup, transitionOutTweenSettings))
                .OnComplete(() =>
                {
                    TransitionState = TransitionState.Idle;
                });
            return transitionSequence;
        }
    }
}
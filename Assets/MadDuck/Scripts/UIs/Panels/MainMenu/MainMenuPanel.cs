using MadDuck.Scripts.Managers;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class MainMenuPanel : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button statsButton;
        [SerializeField] private Button achievementsButton;
        
        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        
        protected override void Awake()
        {
            base.Awake();
            playButton.onClick.AddListener(() => LoadSceneManager.Instance.LoadScene(SceneType.Gameplay, LoadSceneMode.Single, false));
            settingsButton.onClick.AddListener(() => OnButtonClicked(MainMenuPanelType.Settings));
            statsButton.onClick.AddListener(() => OnButtonClicked(MainMenuPanelType.Stats));
            achievementsButton.onClick.AddListener(() => OnButtonClicked(MainMenuPanelType.Achievements));
        }

        private void OnButtonClicked(MainMenuPanelType mainMenuPanelType)
        {
            ChangePanel(MainMenuManager.Instance.PanelDictionary[mainMenuPanelType], crossFadeSettings);
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
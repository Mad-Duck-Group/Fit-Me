using MadDuck.Scripts.Managers;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class MainMenuSettingScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button backButton;
        
        [Title("Tween")]
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        
        public override void Initialize()
        {
            base.Initialize();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        
        private void OnBackButtonClicked()
        {
            var mainMenuPanel = MainMenuManager.Instance.PanelDictionary[MainMenuPanelType.MainMenu];
            ChangePanel(this, mainMenuPanel, CrossFadeSettings);
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
            TransitionState = TransitionState.TransitioningOut;
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
using MadDuck.Scripts.Managers;
using PrimeTween;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class TermsAndConditionsScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private Button acceptButton;
        
        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        
        public override void Initialize()
        {
            base.Initialize();
            acceptButton.onClick.AddListener(OnAcceptButtonClicked);
        }

        private void OnAcceptButtonClicked()
        {
            var loadingPanel = LoadSceneManager.Instance.TransitionScreens.Values.GetRandomElement();
            var mainMenuPanel = MainMenuManager.Instance.PanelDictionary[MainMenuPanelType.MainMenu];
            TransitionScreen(loadingPanel,this, mainMenuPanel);
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
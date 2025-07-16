using Cysharp.Threading.Tasks;
using MadDuck.Scripts.Managers;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.MainMenu
{
    public class SplashScreen : UIPanel
    {
        [Title("References")]
        [SerializeField] private RectTransform madduckLogo;
        [SerializeField] private float splashScreenDuration = 3f;

        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        [SerializeField] private TweenSettings<Vector3> logoScaleTweenSettings;

        private Sequence _logoSequence;

        protected override void Awake()
        {
            base.Awake();
            madduckLogo.localScale = Vector3.zero; // Start with the logo scaled down
        }

        public override Sequence TransitionIn()
        {
            TransitionState = TransitionState.TransitioningIn;
            transitionSequence = Sequence.Create()
                .Group(Tween.Alpha(panelCanvasGroup, transitionInTweenSettings))
                .OnComplete(() =>
                {
                    TweenLogo();
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

        private void TweenLogo()
        {
            _logoSequence = Sequence.Create()
                .Group(Tween.Scale(madduckLogo, logoScaleTweenSettings))
                .OnComplete(OnComplete);
            
            async void OnComplete()
            {
                await UniTask.WaitForSeconds(splashScreenDuration);
                ChangePanel(MainMenuManager.Instance.PanelDictionary[MainMenuPanelType.TermsAndConditions], crossFadeSettings);
            }
        }
        
    }
}
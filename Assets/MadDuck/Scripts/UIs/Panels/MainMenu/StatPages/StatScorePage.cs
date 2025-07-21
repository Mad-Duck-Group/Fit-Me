using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Panels.MainMenu.StatPages
{
    public class StatScorePage : UIPanel
    {
        [Title("Tween")]
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        
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
using System;
using System.Collections.Generic;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Panels.Transition
{
    public interface ITransitionScreen : IUIPanel
    {
        Sequence TransitionBefore();
        Sequence TransitionAfter();
        float Progress { get; set; }
    }
    
    public class BlockCascadeScreen : UIPanel, ITransitionScreen
    {
        [Serializable]
        private struct BlockTween
        {
            public RectTransform block;
            public TweenSettings<Vector2> positionTweenSettings;
        }
        
        [Title("References")]
        [SerializeField] private List<BlockTween> blockTweens;

        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        
        [TitleGroup("Debug")]
        [ShowInInspector, ProgressBar(0f, 1f)] public float Progress { get; set; }
        private Sequence _blockSequence;

        protected override void Awake()
        {
            base.Awake();
            foreach (var blockTween in blockTweens)
            {
                blockTween.block.anchoredPosition = blockTween.positionTweenSettings.startValue;
            }
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

        public Sequence TransitionBefore()
        {
            _blockSequence = Sequence.Create();
            foreach (var blockTween in blockTweens)
            {
                _blockSequence.Chain(Tween.UIAnchoredPosition(blockTween.block, blockTween.positionTweenSettings));
            }
            return _blockSequence;
        }

        public Sequence TransitionAfter()
        {
            _blockSequence = Sequence.Create();
            var reverseTweens = new List<BlockTween>(blockTweens);
            reverseTweens.Reverse();
            foreach (var blockTween in reverseTweens)
            {
                _blockSequence.Chain(Tween.UIAnchoredPosition(blockTween.block,
                    blockTween.positionTweenSettings.WithDirection(false)));
            }
            return _blockSequence;
        }

        
    }
}
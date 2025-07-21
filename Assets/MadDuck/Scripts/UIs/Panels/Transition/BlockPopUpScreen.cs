using System;
using System.Collections.Generic;
using PrimeTween;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MadDuck.Scripts.UIs.Panels.Transition
{
    public class BlockPopUpScreen : UIPanel, ITransitionScreen
    {
        [Serializable]
        private struct BlockTween
        {
            public RectTransform block;
            public TweenSettings<Vector3> scaleTweenSettings;
        }
        
        [Title("References")]
        [SerializeField] private List<BlockTween> blockTweens;

        [Title("Tween")] 
        [SerializeField] private TweenSettings<float> transitionInTweenSettings;
        [SerializeField] private TweenSettings<float> transitionOutTweenSettings;
        
        [TitleGroup("Debug")]
        [ShowInInspector, ProgressBar(0f, 1f)] public float Progress { get; set; }
        private Sequence _blockSequence;

        public override void Initialize()
        {
            base.Initialize();
            foreach (var blockTween in blockTweens)
            {
                blockTween.block.localScale = blockTween.scaleTweenSettings.startValue; // Initialize blocks to their start scale
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
            var randomTweens = new List<BlockTween>(blockTweens);
            foreach (var blockTween in randomTweens.Shuffled())
            {
                _blockSequence.Chain(Tween.Scale(blockTween.block, blockTween.scaleTweenSettings));
            }
            return _blockSequence;
        }

        public Sequence TransitionAfter()
        {
            _blockSequence = Sequence.Create();
            var randomTweens = new List<BlockTween>(blockTweens);
            foreach (var blockTween in randomTweens.Shuffled())
            {
                _blockSequence.Chain(Tween.Scale(blockTween.block,
                    blockTween.scaleTweenSettings.WithDirection(false)));
            }
            return _blockSequence;
        }

        
    }
}
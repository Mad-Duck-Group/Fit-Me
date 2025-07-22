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

        [TitleGroup("Debug")]
        [ShowInInspector, ProgressBar(0f, 1f)] public float Progress { get; set; }
        private Sequence _blockSequence;

        public override void Initialize()
        {
            base.Initialize();
            foreach (var blockTween in blockTweens)
            {
                blockTween.block.anchoredPosition = blockTween.positionTweenSettings.startValue;
            }
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
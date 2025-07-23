using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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

        public async UniTask TransitionBeforeLoad(CancellationToken cancellationToken = default)
        {
            _blockSequence = Sequence.Create();
            var randomTweens = new List<BlockTween>(blockTweens);
            foreach (var blockTween in randomTweens.Shuffled())
            {
                _blockSequence.Chain(Tween.Scale(blockTween.block, blockTween.scaleTweenSettings));
            }
            await _blockSequence.ToUniTask(cancellationToken: cancellationToken);
        }

        public async UniTask TransitionAfterLoad(CancellationToken cancellationToken = default)
        {
            _blockSequence = Sequence.Create();
            var randomTweens = new List<BlockTween>(blockTweens);
            foreach (var blockTween in randomTweens.Shuffled())
            {
                _blockSequence.Chain(Tween.Scale(blockTween.block,
                    blockTween.scaleTweenSettings.WithDirection(false)));
            }
            await _blockSequence.ToUniTask(cancellationToken: cancellationToken);
        }

        public override void CancelTransition()
        {
            base.CancelTransition();
            _blockSequence.Stop();
        }
    }
}
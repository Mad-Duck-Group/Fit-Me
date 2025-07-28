using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Redcode.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private Image background;
        [SerializeField] private TweenSettings<float> backgroundFadeInSettings;
        [SerializeField] private TweenSettings<float> backgroundFadeOutSettings;
        
        [Title("Settings")]
        [SerializeField] private bool useCombinedTime = true;
        [SerializeField, ShowIf(nameof(useCombinedTime))] private float combinedTime = 0.5f;

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
            _blockSequence.Group(Tween.Alpha(background, backgroundFadeInSettings));
            var randomTweens = new List<BlockTween>(blockTweens);
            var scaleDuration = combinedTime / randomTweens.Count;
            Sequence blockSequence = Sequence.Create();
            foreach (var blockTween in randomTweens.Shuffled())
            {
                TweenSettings<Vector3> settings;
                if (useCombinedTime)
                {
                    var copy = blockTween.scaleTweenSettings;
                    copy.settings.duration = scaleDuration;
                    settings = copy;
                }
                else
                {
                    settings = blockTween.scaleTweenSettings;
                }
                blockSequence.Chain(Tween.Scale(blockTween.block, settings));
            }
            _blockSequence.Group(blockSequence);
            await _blockSequence.ToUniTask(cancellationToken: cancellationToken);
        }

        public async UniTask TransitionAfterLoad(CancellationToken cancellationToken = default)
        {
            _blockSequence = Sequence.Create();
            _blockSequence.Group(Tween.Alpha(background, backgroundFadeOutSettings));
            var randomTweens = new List<BlockTween>(blockTweens);
            var scaleDuration = combinedTime / randomTweens.Count;
            Sequence blockSequence = Sequence.Create();
            foreach (var blockTween in randomTweens.Shuffled())
            {
                TweenSettings<Vector3> settings;
                if (useCombinedTime)
                {
                    var copy = blockTween.scaleTweenSettings;
                    copy.settings.duration = scaleDuration;
                    settings = copy;
                }
                else
                {
                    settings = blockTween.scaleTweenSettings;
                }
                blockSequence.Chain(Tween.Scale(blockTween.block,
                    settings.WithDirection(false)));
            }
            _blockSequence.Group(blockSequence);
            await _blockSequence.ToUniTask(cancellationToken: cancellationToken);
        }

        public override void CancelTransition()
        {
            base.CancelTransition();
            _blockSequence.Stop();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace MadDuck.Scripts.UIs.Panels.Transition
{
    public interface ITransitionScreen : IUIPanel
    {
        UniTask TransitionBeforeLoad(CancellationToken cancellationToken = default);
        UniTask TransitionAfterLoad(CancellationToken cancellationToken = default);
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
                blockTween.block.anchoredPosition = blockTween.positionTweenSettings.startValue;
            }
        }

        public async UniTask TransitionBeforeLoad(CancellationToken cancellationToken = default)
        {
            _blockSequence = Sequence.Create();
            _blockSequence.Group(Tween.Alpha(background, backgroundFadeInSettings));
            var duration = combinedTime / blockTweens.Count;
            Sequence blockSequence = Sequence.Create();
            foreach (var blockTween in blockTweens)
            {
                TweenSettings<Vector2> settings;
                if (useCombinedTime)
                {
                    var copy = blockTween.positionTweenSettings;
                    copy.settings.duration = duration;
                    settings = copy;
                }
                else
                {
                    settings = blockTween.positionTweenSettings;
                }
                _blockSequence.Chain(Tween.UIAnchoredPosition(blockTween.block, settings));
            }
            _blockSequence.Group(blockSequence);
            await _blockSequence.ToUniTask(cancellationToken: cancellationToken);
        }

        public async UniTask TransitionAfterLoad(CancellationToken cancellationToken = default)
        {
            var reverseTweens = new List<BlockTween>(blockTweens);
            reverseTweens.Reverse();
            _blockSequence = Sequence.Create();
            _blockSequence.Group(Tween.Alpha(background, backgroundFadeInSettings));
            var duration = combinedTime / reverseTweens.Count;
            Sequence blockSequence = Sequence.Create();
            foreach (var blockTween in reverseTweens)
            {
                TweenSettings<Vector2> settings;
                if (useCombinedTime)
                {
                    var copy = blockTween.positionTweenSettings;
                    copy.settings.duration = duration;
                    settings = copy;
                }
                else
                {
                    settings = blockTween.positionTweenSettings;
                }
                _blockSequence.Chain(Tween.UIAnchoredPosition(blockTween.block, settings.WithDirection(false)));
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